namespace Connected.Caching;

public abstract class CacheContainer<TEntry, TKey> : ICacheContainer<TEntry, TKey> where TEntry : class
{
	protected CacheContainer(ICachingService cachingService, string key)
	{
		if (cachingService is null)
			throw new ArgumentException(nameof(cachingService));

		CachingService = cachingService;
		Key = key;
	}

	protected bool IsDisposed { get; set; }
	protected ICachingService CachingService { get; }
	protected virtual ICollection<string>? Keys => CachingService.Ids(Key);

	public int Count => CachingService.Count(Key);
	public string Key { get; }

	protected async Task Remove(TKey id)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		await CachingService.Remove(Key, id);
	}

	protected async Task Remove(Func<TEntry, bool> predicate)
	{
		await CachingService.Remove(Key, predicate);
	}

	protected virtual Task<ImmutableList<TEntry>?> All()
	{
		return Task.FromResult(CachingService.All<TEntry>(Key));
	}

	protected virtual async Task<TEntry?> Get(TKey id, Func<IEntryOptions, Task<TEntry?>>? retrieve)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		return await CachingService.Get(Key, id, retrieve);
	}

	protected virtual Task<TEntry?> Get(TKey id)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		return Task.FromResult(CachingService.Get<TEntry>(Key, id));
	}

	protected virtual Task<TEntry?> First()
	{
		return Task.FromResult(CachingService.First<TEntry>(Key));
	}

	protected virtual async Task<TEntry?> Get(Func<TEntry, bool> predicate, Func<IEntryOptions, Task<TEntry?>>? retrieve)
	{
		return await CachingService.Get(Key, predicate, retrieve);
	}

	protected virtual async Task<TEntry?> Get(Func<TEntry, bool> predicate)
	{
		return await CachingService.Get(Key, predicate, null);
	}

	protected virtual Task<ImmutableList<TEntry>?> Where(Func<TEntry, bool> predicate)
	{
		return Task.FromResult(CachingService.Where(Key, predicate));
	}

	protected virtual void Set(TKey id, TEntry instance)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		CachingService.Set(Key, id, instance);
	}

	protected virtual void Set(TKey id, TEntry instance, TimeSpan duration)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		CachingService.Set(Key, id, instance, duration);
	}

	protected virtual void Set(TKey id, TEntry instance, TimeSpan duration, bool slidingExpiration)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		CachingService.Set(Key, id, instance, duration, slidingExpiration);
	}

	// protected virtual void Set(TKey id, TEntry instance, TimeSpan duration, bool slidingExpiration, bool performSynchronization)
	// {
	// 	if (id is null)
	// 		throw new ArgumentNullException(nameof(id));

	// 	CachingService.Set(Key, id, instance, duration, slidingExpiration, performSynchronization);
	// }

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
				OnDisposing();

			IsDisposed = true;
		}
	}

	protected virtual void OnDisposing()
	{

	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public virtual IEnumerator<TEntry> GetEnumerator()
	{
		var result = CachingService?.GetEnumerator<TEntry>(Key);

		if (result is null)
			throw new NullReferenceException("Cannot retrieve cache enumerator.");

		return result;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
