using Connected.Reflection;
using Connected.Storage.Transactions;
using System.Collections.Immutable;
using System.Reflection;

namespace Connected.Caching;

internal class CacheContext : Cache, ICacheContext
{
	public CacheContext(ICachingService cachingService, ITransactionContext transactionContext)
	{
		CachingService = cachingService;
		TransactionContext = transactionContext;

		TransactionContext.StateChanged += OnTransactionContextStateChanged;
	}

	private ICachingService CachingService { get; }
	private ITransactionContext TransactionContext { get; }

	private void OnTransactionContextStateChanged(object? sender, EventArgs e)
	{
		if (TransactionContext.State == MiddlewareTransactionState.Committing)
			Flush();
	}

	public override bool Exists(string key)
	{
		return base.Exists(key) || CachingService is not null && CachingService.Exists(key);
	}

	public override bool IsEmpty(string key)
	{
		return base.IsEmpty(key) || CachingService is not null && CachingService.IsEmpty(key);
	}

	public override ImmutableList<T>? All<T>(string key)
	{
		return Merge(base.All<T>(key), CachingService?.All<T>(key));
	}

	public override async Task<T?> Get<T>(string key, object id, Func<IEntryOptions, Task<T?>>? retrieve) where T : default
	{
		if (!TransactionContext.IsDirty)
		{
			if (retrieve is null)
				return default;

			return await CachingService.Get(key, id, retrieve);
		}

		return await base.Get(key, id, (f) =>
		{
			var shared = CachingService.Get<T>(key, id);

			if (shared is not null || retrieve is null)
				return Task.FromResult(shared);

			return retrieve(f);
		});
	}

	public override async Task<T?> Get<T>(string key, Func<T, bool> predicate, Func<IEntryOptions, Task<T?>>? retrieve) where T : default
	{
		if (!TransactionContext.IsDirty)
		{
			if (retrieve is null)
				return default;

			return await CachingService.Get(key, predicate, retrieve);
		}

		return await base.Get(key, predicate, (f) =>
		{
			var shared = CachingService.Get(key, predicate);

			if (shared is not null || retrieve is null)
				return Task.FromResult(shared);

			return retrieve(f);
		});
	}

	public override T? Get<T>(string key, object id) where T : default
	{
		var contextItem = base.Get<T>(key, id);

		if (contextItem is not null)
			return contextItem;

		return CachingService.Get<T>(key, id);
	}

	public override T? Get<T>(string key, Func<T, bool> predicate) where T : default
	{
		var contextItem = base.Get(key, predicate);

		if (contextItem is not null)
			return contextItem;

		return CachingService.Get(key, predicate);
	}

	public override async Task Clear(string key)
	{
		await base.Clear(key);
		await CachingService.Clear(key);
	}

	public override T? First<T>(string key) where T : default
	{
		if (base.First<T>(key) is T result)
			return result;

		return CachingService.First<T>(key);
	}

	public override ImmutableList<T>? Where<T>(string key, Func<T, bool> predicate)
	{
		return Merge(base.Where(key, predicate), CachingService.Where(key, predicate));
	}

	public override T? Set<T>(string key, object id, T? instance) where T : default
	{
		if (!TransactionContext.IsDirty)
			return CachingService.Set(key, id, instance);

		return base.Set(key, id, instance);
	}

	public override T? Set<T>(string key, object id, T? instance, TimeSpan duration) where T : default
	{
		if (!TransactionContext.IsDirty)
			return CachingService.Set(key, id, instance, duration);

		return base.Set(key, id, instance, duration);
	}

	public override T? Set<T>(string key, object id, T? instance, TimeSpan duration, bool slidingExpiration) where T : default
	{
		if (!TransactionContext.IsDirty)
			return CachingService.Set(key, id, instance, duration, slidingExpiration);

		return base.Set(key, id, instance, duration, slidingExpiration);
	}

	public override async Task Remove(string key, object id)
	{
		await base.Remove(key, id);
		await CachingService.Remove(key, id);
	}

	public override async Task<ImmutableList<string>?> Remove<T>(string key, Func<T, bool> predicate)
	{
		var local = await base.Remove(key, predicate);
		var shared = await CachingService.Remove(key, predicate);

		if (local is not null && shared is not null)
			return local.AddRange(shared);

		return local is not null ? local : shared;
	}

	public void Flush()
	{
		CachingService.Merge(this);
	}

	private static ImmutableList<T>? Merge<T>(ImmutableList<T>? contextItems, ImmutableList<T>? sharedItems)
	{
		if (contextItems is null)
			return sharedItems;

		if (sharedItems is null)
			return default;

		var result = new List<T>(contextItems);

		foreach (var sharedItem in sharedItems)
		{
			if (sharedItem is null)
				continue;

			if (CachingUtils.GetCacheKeyProperty(sharedItem) is not PropertyInfo cacheProperty)
			{
				//Q: should we compare every property and add only if not matched?
				contextItems.Add(sharedItem);
				continue;
			}

			if (FindExisting(cacheProperty.GetValue(sharedItems), contextItems) is null)
				result.Add(sharedItem);
		}

		return [.. result];
	}

	private static T? FindExisting<T>(object? value, ImmutableList<T> items)
	{
		if (items is null || items.IsEmpty)
			return default;

		var instance = items[0];

		if (instance is null)
			return default;

		if (CachingUtils.GetCacheKeyProperty(instance) is not PropertyInfo cacheProperty)
			return default;

		foreach (var item in items)
		{
			var id = cacheProperty.GetValue(item);

			if (TypeComparer.Compare(id, value))
				return item;
		}

		return default;
	}
}