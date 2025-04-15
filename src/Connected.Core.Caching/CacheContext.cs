using Connected.Reflection;
using Connected.Storage.Transactions;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Caching;

internal class CacheContext : ICacheContext
{
	private readonly CacheScope _scope = new();
	private readonly Dictionary<string, HashSet<object?>> _removeList = [];
	public CacheContext(ICachingService cache, ITransactionContext transactions)
	{
		Cache = cache;
		Transactions = transactions;

		Transactions.StateChanged += OnTransactionContextStateChanged;
	}

	private ICachingService Cache { get; }
	private ITransactionContext Transactions { get; }
	private void OnTransactionContextStateChanged(object? sender, EventArgs e)
	{
		if (Transactions.State == MiddlewareTransactionState.Completed)
			Flush();
	}

	public bool Exists(string key)
	{
		return _scope.Exists(key) || Cache.Exists(key);
	}

	public bool IsEmpty(string key)
	{
		if (!_scope.IsEmpty(key))
			return false;

		if (_removeList.Count == 0)
			return Cache.IsEmpty(key);

		var items = Cache.All<object>(key);

		if (items is null || items.Count == 0)
			return true;

		foreach (var item in items)
		{
			var id = ResolveId(item);

			if (!IsRemoved(key, id))
				return false;
		}

		return true;
	}

	public IImmutableList<T>? All<T>(string key)
	{
		return Merge(_scope.All<T>(key), Cache?.All<T>(key));
	}

	public async Task<T?> Get<T>(string key, object id, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		if (_scope.Get(key, id) is T scoped)
			return scoped;

		if (!IsRemoved(key, id))
		{
			if (Cache.Get(key, id) is T shared)
				return shared;
		}

		return await _scope.Get(key, id, async (f) =>
		{
			if (retrieve is not null)
				return await retrieve(f);

			return default;
		});
	}

	public async Task<T?> Get<T>(string key, Func<T, bool> predicate, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		if (_scope.Get(key, predicate) is T scoped)
			return scoped;

		if (Cache.Get(key, predicate) is T shared)
		{
			if (!IsItemRemoved(key, shared))
				return shared;
		}

		return await _scope.Get(key, predicate, async (f) =>
		{
			if (retrieve is not null)
				return await retrieve(f);

			return default;
		});
	}

	public T? Get<T>(string key, object id)
	{
		if (_scope.Get<T>(key, id) is T scoped)
			return scoped;

		if (!IsRemoved(key, id))
			return Cache.Get<T>(key, id);

		return default;
	}

	public T? Get<T>(string key, Func<T, bool> predicate)
	{
		if (_scope.Get(key, predicate) is T scoped)
			return scoped;

		var result = Cache.Get(key, predicate);

		if (result is null)
			return result;

		if (!IsItemRemoved(key, result))
			return result;

		return default;
	}

	public async Task Clear(string key)
	{
		await _scope.Clear(key);
		//TODO: this should be optimized because we should perform clear on commit only.
		await Cache.Clear(key);
	}

	public T? First<T>(string key)
	{
		if (_scope.First<T>(key) is T result)
			return result;

		var shared = Cache.First<T>(key);

		if (shared is null)
			return shared;

		if (!IsItemRemoved(key, shared))
			return shared;

		var items = Cache.All<T>(key);

		if (items is null || items.Count == 0)
			return default;

		foreach (var item in items)
		{
			if (IsItemRemoved(key, item))
				continue;

			return item;
		}

		return default;
	}

	public IImmutableList<T>? Where<T>(string key, Func<T, bool> predicate)
	{
		return Merge(_scope.Where(key, predicate), Cache.Where(key, predicate));
	}

	public T? Set<T>(string key, object id, T? instance)
	{
		RemoveFromRemoveList(key, id);

		return _scope.Set(key, id, instance);
	}

	public T? Set<T>(string key, object id, T? instance, TimeSpan duration)
	{
		RemoveFromRemoveList(key, id);

		return _scope.Set(key, id, instance, duration);
	}

	public T? Set<T>(string key, object id, T? instance, TimeSpan duration, bool slidingExpiration)
	{
		RemoveFromRemoveList(key, id);

		return _scope.Set(key, id, instance, duration, slidingExpiration);
	}

	public async Task Remove(string key, object id)
	{
		await _scope.Remove(key, id);

		AddToRemoveList(key, id);
	}

	public async Task<IImmutableList<string>?> Remove<T>(string key, Func<T, bool> predicate)
	{
		var items = await _scope.Remove(key, predicate);

		if (items is not null)
		{
			foreach (var item in items)
			{
				var id = ResolveId(item);

				if (id is not null)
					AddToRemoveList(key, id);
			}
		}

		return items;
	}

	public void Flush()
	{
		foreach (var item in _removeList)
		{
			foreach (var entry in item.Value)
			{
				if (entry is null)
					continue;

				Cache.Remove(item.Key, entry).Wait();
			}
		}

		Cache.Merge(this);
	}

	private static IImmutableList<T>? Merge<T>(IImmutableList<T>? scope, IImmutableList<T>? shared)
	{
		if (scope is null)
			return shared;

		if (shared is null)
			return default;

		foreach (var sharedItem in shared)
		{
			if (sharedItem is null)
				continue;

			if (FindExisting(sharedItem, scope) is not T existing)
				scope = scope.Add(sharedItem);
		}

		return [.. scope];
	}

	private static T? FindExisting<T>(object? value, IImmutableList<T> items)
	{
		if (items.Count == 0)
			return default;

		foreach (var item in items)
		{
			var id = ResolveId(item);

			if (TypeComparer.Compare(id, value))
				return item;
		}

		return default;
	}

	private static object? ResolveId(object? value)
	{
		if (value is null)
			return default;

		if (CachingUtils.GetCacheKeyProperty(value) is not PropertyInfo cacheProperty)
			return default;

		return cacheProperty.GetValue(value);
	}

	public IEntry? Get(string key, object id)
	{
		if (_scope.Get(key, id) is IEntry scoped)
			return scoped;

		if (IsRemoved(key, id))
			return default;

		return Cache.Get(key, id);
	}

	public IEnumerator<T>? GetEnumerator<T>(string key)
	{
		throw new NotImplementedException();
	}

	public int Count(string key)
	{
		var merged = Merge(_scope.All<object>(key), Cache.All<object>(key));

		if (merged is null)
			return 0;

		return merged.Count;
	}

	public void CopyTo(string key, object id, IEntry entry)
	{
		_scope.CopyTo(key, id, entry);
	}

	public IImmutableList<string>? Ids(string key)
	{
		var scoped = _scope.Ids(key) ?? [];
		var shared = Cache.Ids(key) ?? [];

		if (shared.Count == 0)
			return scoped;

		if (_removeList.Count == 0)
			return [.. scoped.AddRange(shared).Distinct()];

		_removeList.TryGetValue(key, out HashSet<object?>? removeSet);

		foreach (var item in shared)
		{
			if (removeSet is not null)
			{
				if (removeSet.Any(f => f is not null && string.Equals(f.ToString(), item, StringComparison.Ordinal)))
					continue;
			}

			scoped = scoped.Add(item);
		}

		return scoped;
	}

	public IImmutableList<string>? Keys()
	{
		var scoped = _scope.Keys();
		var shared = Cache.Keys();

		return [.. scoped.AddRange(shared ?? []).Distinct()];
	}

	public void Dispose()
	{
		_scope.Dispose();
	}

	private bool IsItemRemoved(string key, object? item)
	{
		if (item is null)
			return false;

		if (!_removeList.ContainsKey(key))
			return false;

		return IsRemoved(key, ResolveId(key));
	}

	private bool IsRemoved(string key, object? id)
	{
		if (!_removeList.ContainsKey(key))
			return false;

		if (_removeList.TryGetValue(key, out HashSet<object?>? items))
			return items.Contains(id);

		return false;
	}

	private void RemoveFromRemoveList(string key, object id)
	{
		if (_removeList.TryGetValue(key, out HashSet<object?>? existing))
			existing.Remove(id);
	}

	private void AddToRemoveList(string key, object id)
	{
		if (_removeList.TryGetValue(key, out HashSet<object?>? existing))
		{
			if (existing.Contains(id))
				return;

			existing.Add(id);
		}

		lock (_removeList)
		{
			if (_removeList.TryGetValue(key, out HashSet<object?>? locked))
			{
				if (locked.Contains(id))
					return;

				locked.Add(id);
			}
			else
				_removeList.Add(key, [id]);
		}
	}
}