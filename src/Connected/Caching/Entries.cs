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
	 * One view per type the bucket has been queried as. A view holds the current snapshot of the bucket as that type and
	 * is brought up to date under the bucket's lock by every change to Items, at O(log n) a change, so a query never
	 * copies the bucket and a finished change is seen by the next query. The map is replaced, never changed, when a view
	 * is added or dropped, so readers look it up without the lock.
	 */
	private volatile Dictionary<Type, IView> _views = [];

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

			Removed(key);
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
	/// The bucket's instances as <typeparamref name="T"/>, built once under the bucket's lock and then kept up to date by
	/// every change. Nobody can change a snapshot once it is handed out; a change makes a new one that shares the rest.
	/// </summary>
	private Snapshot<T> GetSnapshot<T>()
	{
		if (_views.TryGetValue(typeof(T), out var view) && ((View<T>)view).Current is Snapshot<T> current)
			return current;

		lock (this)
		{
			if (_views.TryGetValue(typeof(T), out view) && ((View<T>)view).Current is Snapshot<T> built)
				return built;

			var created = View<T>.Build(Items);

			_views = new Dictionary<Type, IView>(_views) { [typeof(T)] = created };

			return created.Current!;
		}
	}

	/// <summary>
	/// Brings every view up to date with the key's new instance, or with its removal when <paramref name="removed"/> is set.
	/// Called under the bucket's lock. A view whose type the instance is not is dropped, and the next query as that type
	/// builds it again from Items (and throws, as it should, while such an instance is in the bucket). A view that fails
	/// to take the change for any other reason is dropped too, rather than left out of step with Items.
	/// </summary>
	private void Changed(string key, object? instance, bool removed = false)
	{
		var views = _views;

		if (views.Count == 0)
			return;

		List<Type>? dropped = null;

		foreach (var i in views)
		{
			var applied = false;

			try
			{
				if (removed)
				{
					i.Value.Remove(key);
					applied = true;
				}
				else
					applied = i.Value.Set(key, instance);
			}
			catch
			{
			}

			if (applied)
				continue;

			i.Value.Invalidate();
			(dropped ??= []).Add(i.Key);
		}

		if (dropped is not null)
			Drop(dropped);
	}

	private void Removed(string key)
	{
		Changed(key, null, true);
	}

	private void Drop(List<Type> types)
	{
		var views = new Dictionary<Type, IView>(_views);

		foreach (var i in types)
			views.Remove(i);

		_views = views;
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
				Items[key] = e = new Entry(key, instance, duration, slidingExpiration, merge);
			/*
			 * The entry's instance after the write, which is the old one when the entry refused a stale version.
			 */
			Changed(key, e.Instance);
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
		var slots = GetSnapshot<T>().Slots;

		if (slots.Count == 0)
			return ImmutableList<T>.Empty;

		var filtered = ImmutableList.CreateBuilder<T>();

		for (var page = 0; page < slots.PageCount; page++)
		{
			foreach (var i in slots.Page(page))
			{
				if (predicate(i.Item))
					filtered.Add(i.Item);
			}
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

			Removed(key);
		}

		d.Dispose();
	}

	private IEntry? Find<T>(Func<T, bool> predicate)
	{
		var slots = GetSnapshot<T>().Slots;

		for (var page = 0; page < slots.PageCount; page++)
		{
			foreach (var i in slots.Page(page))
			{
				if (!predicate(i.Item))
					continue;

				if (i.Item is not T instance)
					return default;

				HitList.Enqueue(instance);
				/*
				 * The entry under the matched key, as long as it still holds the instance the predicate matched.
				 */
				if (Items.TryGetValue(i.Key, out IEntry? entry) && InstanceEquals(entry.Instance, instance))
					return entry;

				return Items.Values.FirstOrDefault(f => InstanceEquals(f.Instance, instance));
			}
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

	private interface IView
	{
		/// <summary>
		/// False when the instance is not of the view's type, and the view cannot hold it.
		/// </summary>
		bool Set(string key, object? instance);
		void Remove(string key);
		void Invalidate();
	}

	/// <summary>
	/// The bucket as <typeparamref name="T"/>. Only writers, under the bucket's lock, touch the index of the keys and
	/// make new snapshots; readers only read <see cref="Current"/>. A removed key's slot is filled with the last one, so
	/// the order of the snapshot is the order of the writes only until the first removal; it was never defined.
	/// </summary>
	private sealed class View<T> : IView
	{
		private readonly Dictionary<string, int> _index;
		private volatile Snapshot<T>? _current;

		private View(Snapshot<T> current, Dictionary<string, int> index)
		{
			_current = current;
			_index = index;
		}

		/// <summary>
		/// The bucket now, or null once the view is dropped.
		/// </summary>
		public Snapshot<T>? Current => _current;

		public static View<T> Build(ConcurrentDictionary<string, IEntry> entries)
		{
			var items = ImmutableList.CreateBuilder<T>();
			var slots = new List<Slot<T>>(entries.Count);
			var index = new Dictionary<string, int>(entries.Count);
			var nulls = 0;

			foreach (var i in entries)
			{
				if (!TryCast(i.Value.Instance, out var item, out var isNull))
					throw new InvalidCastException($"{Exceptions.ExInvalidCacheEntry} ({i.Value.Instance?.GetType().ShortName()}->{typeof(T).ShortName()})");

				if (isNull)
					nulls++;

				index[i.Key] = items.Count;
				items.Add(item);
				slots.Add(new Slot<T>(i.Key, item));
			}

			return new View<T>(new Snapshot<T>(items.ToImmutable(), Pages<Slot<T>>.Create(slots), nulls), index);
		}

		public bool Set(string key, object? instance)
		{
			if (!TryCast(instance, out var item, out var isNull))
				return false;

			var current = _current!;
			var slot = new Slot<T>(key, item);

			if (_index.TryGetValue(key, out var index))
			{
				var nulls = current.Nulls - (current.Slots[index].Item is null ? 1 : 0) + (isNull ? 1 : 0);

				_current = new Snapshot<T>(current.List.SetItem(index, item), current.Slots.SetItem(index, slot), nulls);
			}
			else
			{
				_index[key] = current.List.Count;
				_current = new Snapshot<T>(current.List.Add(item), current.Slots.Add(slot), current.Nulls + (isNull ? 1 : 0));
			}

			return true;
		}

		public void Remove(string key)
		{
			if (!_index.Remove(key, out var index))
				return;

			var current = _current!;
			var last = current.Slots.Count - 1;
			var nulls = current.Nulls - (current.Slots[index].Item is null ? 1 : 0);
			var list = current.List;
			var slots = current.Slots;

			if (index != last)
			{
				var moved = slots[last];

				list = list.SetItem(index, moved.Item);
				slots = slots.SetItem(index, moved);
				_index[moved.Key] = index;
			}

			_current = new Snapshot<T>(list.RemoveAt(last), slots.RemoveLast(), nulls);
		}

		public void Invalidate()
		{
			_current = null;
		}

		private static bool TryCast(object? instance, out T item, out bool isNull)
		{
			isNull = false;

			if (instance is T t)
			{
				item = t;
				return true;
			}

			item = default!;

			if (instance is null && default(T) is null)
			{
				isNull = true;
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// One point in time of the bucket as <typeparamref name="T"/>: the list handed out by All, and the same items with
	/// their keys in pages that Where and Get scan at array speed (enumerating the list's tree is many times slower).
	/// </summary>
	private sealed class Snapshot<T>(ImmutableList<T> list, Pages<Slot<T>> slots, int nulls)
	{
		public ImmutableList<T> List { get; } = list;
		public Pages<Slot<T>> Slots { get; } = slots;
		public int Nulls { get; } = nulls;
		public bool HasNull => Nulls > 0;
	}

	private readonly record struct Slot<T>(string Key, T Item);

	/// <summary>
	/// A persistent array in pages of 256: a change copies the page it lands in and the table of pages, never the whole
	/// array, and leaves the old one as it was.
	/// </summary>
	private sealed class Pages<TItem>
	{
		private const int Shift = 8;
		private const int Size = 1 << Shift;
		private const int Mask = Size - 1;

		private readonly TItem[][] _pages;

		private Pages(TItem[][] pages, int count)
		{
			_pages = pages;
			Count = count;
		}

		public int Count { get; }
		public int PageCount => _pages.Length;
		public TItem this[int index] => _pages[index >> Shift][index & Mask];

		public ReadOnlySpan<TItem> Page(int page)
		{
			return _pages[page].AsSpan(0, Math.Min(Size, Count - (page << Shift)));
		}

		public static Pages<TItem> Create(List<TItem> items)
		{
			var pages = new TItem[(items.Count + Mask) >> Shift][];

			for (var i = 0; i < pages.Length; i++)
			{
				var page = new TItem[Size];
				var start = i << Shift;

				items.CopyTo(start, page, 0, Math.Min(Size, items.Count - start));
				pages[i] = page;
			}

			return new Pages<TItem>(pages, items.Count);
		}

		public Pages<TItem> SetItem(int index, TItem value)
		{
			var pages = (TItem[][])_pages.Clone();
			var page = (TItem[])pages[index >> Shift].Clone();

			page[index & Mask] = value;
			pages[index >> Shift] = page;

			return new Pages<TItem>(pages, Count);
		}

		public Pages<TItem> Add(TItem value)
		{
			var at = Count >> Shift;
			TItem[][] pages;
			TItem[] page;

			if (at == _pages.Length)
			{
				pages = new TItem[at + 1][];
				Array.Copy(_pages, pages, at);
				page = new TItem[Size];
			}
			else
			{
				pages = (TItem[][])_pages.Clone();
				page = (TItem[])pages[at].Clone();
			}

			page[Count & Mask] = value;
			pages[at] = page;

			return new Pages<TItem>(pages, Count + 1);
		}

		public Pages<TItem> RemoveLast()
		{
			var last = Count - 1;
			var at = last >> Shift;

			if ((last & Mask) == 0)
				return new Pages<TItem>(_pages[..at], last);

			var pages = (TItem[][])_pages.Clone();
			var page = (TItem[])pages[at].Clone();
			/*
			 * Cleared so the page does not keep the removed instance alive.
			 */
			page[last & Mask] = default!;
			pages[at] = page;

			return new Pages<TItem>(pages, last);
		}
	}
}
