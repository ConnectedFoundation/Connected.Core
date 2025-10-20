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
	public int Count => Items.Count;

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
			Remove(i);
	}

	public IImmutableList<T> All<T>()
	{
		var r = new List<T>();
		var instances = Items.Select(f => f.Value.Instance);

		foreach (var i in instances)
		{
			if (i is not T t)
				throw new InvalidCastException($"{Exceptions.ExInvalidCacheEntry} ({i?.GetType().ShortName()}->{typeof(T).ShortName()})");

			r.Add(t);
		}

		return [.. r];
	}

	public void Remove(string key)
	{
		if (Items.IsEmpty)
			return;

		if (Items.TryRemove(key, out IEntry? v))
			v.Dispose();
	}

	public void Set(string key, object? instance, TimeSpan duration, bool slidingExpiration)
	{
		Items[key] = new Entry(key, instance, duration, slidingExpiration);
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
		var values = Items.Select(f => f.Value.Instance).Cast<T>();

		if (values is null || !values.Any())
			return ImmutableList<T>.Empty;

		var filtered = values.Where(predicate);

		if (filtered is null || !filtered.Any())
			return ImmutableList<T>.Empty;

		foreach (var i in filtered)
			HitList.Enqueue(i);

		return filtered.ToImmutableList();
	}

	private void RemoveInternal(string key)
	{
		if (Items.TryRemove(key, out IEntry? d))
			d.Dispose();
	}

	private IEntry? Find<T>(Func<T, bool> predicate)
	{
		var instances = Items.Select(f => f.Value?.Instance).Cast<T>();

		if (instances is null || !instances.Any())
			return default;

		if (instances.FirstOrDefault(predicate) is not T instance)
			return default;

		HitList.Enqueue(instance);

		return Items.Values.FirstOrDefault(f => InstanceEquals(f.Instance, instance));
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
}