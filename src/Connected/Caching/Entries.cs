using Connected.Reflection;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Caching;

internal class Entries
{
	public Entries()
	{
		Items = new();
		HitList = new();
	}

	private ConcurrentDictionary<string, IEntry> Items { get; }
	private ConcurrentQueue<object?> HitList { get; }
	public IImmutableList<string> Keys => [.. Items.Keys];
	/// <summary>
	/// Set under the bucket's lock when the cleanup takes the bucket out of the cache, so a writer holding it
	/// knows to fetch the key's bucket again instead of writing into one that is gone.
	/// </summary>
	public bool Retired { get; set; }
	public int Count => Items.Count;
	/*
	 * Bumped under the bucket's lock after every change to Items. A snapshot built at one version is handed out until
	 * the version moves on, so a query reads the bucket without copying it, and a change is seen by the next query.
	 */
	private long _version;
	private volatile Snapshots _snapshots = new(-1);

	public bool Any()
	{
		return !Items.IsEmpty;
	}

	public void Scave()
	{
		while (HitList.TryDequeue(out object? instance))
		{
			if (instance is null)
				continue;

			var target = Items.FirstOrDefault(f => f.Value.Instance == instance);

			if (target.Value is not null && target.Value is Entry entry)
			{
				if (entry.SlidingExpiration && entry.Duration > TimeSpan.Zero)
					entry.ExpirationDate = DateTime.UtcNow.AddTicks(entry.Duration.Ticks);
			}
		}

		var expired = new HashSet<string>();

		foreach (var i in Items)
		{
			var r = i.Value;

			if (r is null || r.Expired)
				expired.Add(i.Key);
		}

		foreach (var i in expired)
			RemoveExpired(i);
	}

	/// <summary>
	/// Removes the entry only if it is still expired, so a write that refreshed it after the scan is kept.
	/// </summary>
	private void RemoveExpired(string key)
	{
		IEntry? d;

		lock (this)
		{
			if (!Items.TryGetValue(key, out d) || (d is not null && !d.Expired) || !Items.TryRemove(key, out d))
				return;

			Changed();
		}

		d?.Dispose();
	}

	public IImmutableList<T> All<T>()
	{
		var snapshot = GetSnapshot<T>();

		if (snapshot.HasNull)
			throw new InvalidCastException($"{Exceptions.ExInvalidCacheEntry} (null->{typeof(T).ShortName()})");

		return snapshot.List;
	}

	/// <summary>
	/// The bucket's instances as <typeparamref name="T"/> at the current version, built once under the bucket's lock and
	/// shared by every reader until the next change. Nobody can change a snapshot once it is handed out.
	/// </summary>
	private Snapshot<T> GetSnapshot<T>()
	{
		var current = _snapshots;

		if (current.Version == Volatile.Read(ref _version) && current.Typed.TryGetValue(typeof(T), out var cached))
			return (Snapshot<T>)cached;

		lock (this)
		{
			current = _snapshots;

			if (current.Version != _version)
			{
				current = new Snapshots(_version);
				_snapshots = current;
			}
			else if (current.Typed.TryGetValue(typeof(T), out cached))
				return (Snapshot<T>)cached;

			var result = BuildSnapshot<T>();

			current.Typed[typeof(T)] = result;

			return result;
		}
	}

	private Snapshot<T> BuildSnapshot<T>()
	{
		var items = new T[Items.Count];
		var keys = new string[items.Length];
		var hasNull = false;
		var index = 0;

		foreach (var i in Items)
		{
			var instance = i.Value.Instance;

			if (instance is T t)
				items[index] = t;
			else if (instance is null && default(T) is null)
			{
				items[index] = default!;
				hasNull = true;
			}
			else
				throw new InvalidCastException($"{Exceptions.ExInvalidCacheEntry} ({instance?.GetType().ShortName()}->{typeof(T).ShortName()})");

			keys[index] = i.Key;
			index++;
		}

		return new Snapshot<T>(items, keys, hasNull);
	}

	private void Changed()
	{
		Interlocked.Increment(ref _version);
	}

	public void Remove(string key)
	{
		if (Items.IsEmpty)
			return;

		RemoveInternal(key);
	}

	public void Set(string key, object? instance, TimeSpan duration, bool slidingExpiration, CacheEntryMergeBehavior merge)
	{
		lock (this)
		{
			if (Items.TryGetValue(key, out IEntry? existing) && existing is Entry e)
				e.Set(instance, duration, slidingExpiration, merge);
			else
				Items[key] = new Entry(key, instance, duration, slidingExpiration, merge);

			Changed();
		}
	}

	public IEnumerator<T> GetEnumerator<T>()
	{
		return new EntryEnumerator<T>(Items);
	}

	public IEntry? Get(string key)
	{
		return Find(key);
	}

	public IEntry? First()
	{
		if (!Any())
			return default;

		return Items.First().Value;
	}

	public IEntry? Get<T>(Func<T, bool> predicate)
	{
		return Find(predicate);
	}

	public IImmutableList<string> Remove<T>(Func<T, bool> predicate)
	{
		if (Where(predicate) is not IImmutableList<T> ds || ds.Count == 0)
			return ImmutableList<string>.Empty;

		var result = new HashSet<string>();

		foreach (var i in ds)
		{
			var key = Items.FirstOrDefault(f => InstanceEquals(f.Value?.Instance, i)).Key;

			RemoveInternal(key);

			result.Add(key);
		}

		return [.. result];
	}

	public IImmutableList<T> Where<T>(Func<T, bool> predicate)
	{
		var items = GetSnapshot<T>().Items;

		if (items.Length == 0)
			return ImmutableList<T>.Empty;

		var filtered = ImmutableList.CreateBuilder<T>();

		foreach (var i in items)
		{
			if (predicate(i))
				filtered.Add(i);
		}

		if (filtered.Count == 0)
			return ImmutableList<T>.Empty;

		foreach (var i in filtered)
			HitList.Enqueue(i);

		return filtered.ToImmutable();
	}

	private void RemoveInternal(string key)
	{
		IEntry? d;

		lock (this)
		{
			if (!Items.TryRemove(key, out d))
				return;

			Changed();
		}

		d.Dispose();
	}

	private IEntry? Find<T>(Func<T, bool> predicate)
	{
		var snapshot = GetSnapshot<T>();
		var items = snapshot.Items;

		for (var i = 0; i < items.Length; i++)
		{
			if (!predicate(items[i]))
				continue;

			if (items[i] is not T instance)
				return default;

			HitList.Enqueue(instance);
			/*
			 * The entry under the matched key, as long as it still holds the instance the predicate matched.
			 */
			if (Items.TryGetValue(snapshot.Keys[i], out IEntry? entry) && InstanceEquals(entry.Instance, instance))
				return entry;

			return Items.Values.FirstOrDefault(f => InstanceEquals(f.Instance, instance));
		}

		return default;
	}

	private IEntry? Find(string key)
	{
		if (!Items.ContainsKey(key))
			return default;

		if (Items.TryGetValue(key, out IEntry? d))
		{
			if (d is null)
				return default;

			HitList.Enqueue(d.Instance);

			return d;
		}

		return default;
	}

	public bool Exists(string key)
	{
		return Find(key) is not null;
	}

	public void Clear()
	{
		foreach (var i in Items)
			Remove(i.Key);
	}

	private static bool InstanceEquals(object? left, object? right)
	{
		/*
		 * TODO: implement IEquality check
		 */
		if (left is null || right is null)
			return false;

		if (left.GetType().IsPrimitive)
			return left == right;

		if (left is string && right is string)
			return string.Compare(left.ToString(), right.ToString(), false) == 0;

		if (left.GetType().IsValueType && right.GetType().IsValueType)
			return left.Equals(right);

		return ReferenceEqualityComparer.Instance.Equals(left, right);
	}

	private sealed class Snapshots(long version)
	{
		public long Version { get; } = version;
		public ConcurrentDictionary<Type, object> Typed { get; } = new();
	}

	private sealed class Snapshot<T>(T[] items, string[] keys, bool hasNull)
	{
		private ImmutableList<T>? _list;

		public T[] Items { get; } = items;
		public string[] Keys { get; } = keys;
		public bool HasNull { get; } = hasNull;
		public ImmutableList<T> List
		{
			get
			{
				if (_list is null)
					Interlocked.CompareExchange(ref _list, ImmutableList.Create(Items), null);

				return _list;
			}
		}
	}
}
