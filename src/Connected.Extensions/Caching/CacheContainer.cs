using Connected.Reflection;
using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Connected.Caching;

public abstract class CacheContainer<TEntry, TKey> : ICacheContainer<TEntry, TKey>
{
	protected CacheContainer(ICachingService cachingService, string key)
	{
		if (cachingService is null)
			ArgumentNullException.ThrowIfNull(cachingService);

		CachingService = cachingService;
		Key = key;

		Context = cachingService.CreateContext();
	}

	private ICacheContext Context { get; }
	protected bool IsDisposed { get; set; }
	protected ICachingService CachingService { get; }
	public virtual IImmutableList<string> Keys => Context.Ids(Key);

	public int Count => Context.Count(Key);
	public string Key { get; }

	public async Task Remove(TKey id)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		await Context.Remove(Key, id);

		await OnRemoved(id);
	}

	public async Task Remove(Func<TEntry, bool> predicate)
	{
		var removed = await Context.Remove(Key, predicate);

		if (removed is null)
			return;

		foreach (var item in removed)
		{
			var converted = Types.Convert<TKey>(item) ?? throw new NullReferenceException();

			await OnRemoved(converted);
		}
	}

	protected virtual async Task OnRemoved(TKey id)
	{
		await Task.CompletedTask;
	}

	public virtual Task<IImmutableList<TEntry>> All()
	{
		return Task.FromResult(Context.All<TEntry>(Key));
	}

	public virtual async Task<TEntry?> Get(TKey id, Func<IEntryOptions, Task<TEntry?>>? retrieve)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		return await Context.Get(Key, id, retrieve);
	}

	public virtual Task<TEntry?> Get(TKey id)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		return Task.FromResult(Context.Get<TEntry>(Key, id));
	}

	public virtual Task<TEntry?> First()
	{
		return Task.FromResult(Context.First<TEntry>(Key));
	}

	public virtual async Task<TEntry?> Get(Func<TEntry, bool> predicate, Func<IEntryOptions, Task<TEntry?>>? retrieve)
	{
		return await Context.Get(Key, predicate, retrieve);
	}

	public virtual async Task<TEntry?> Get(Func<TEntry, bool> predicate)
	{
		return await Context.Get(Key, predicate, null);
	}

	public virtual void Set(TKey id, TEntry instance)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		Context.Set(Key, id, instance);
	}

	public virtual void Set(TKey id, TEntry instance, TimeSpan duration)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		Context.Set(Key, id, instance, duration);
	}

	public virtual void Set(TKey id, TEntry instance, TimeSpan duration, bool slidingExpiration)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		Context.Set(Key, id, instance, duration, slidingExpiration);
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				Context.Dispose();
				OnDisposing();
			}

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
		var enumerator = Context?.GetEnumerator<TEntry>(Key);

		if (enumerator is not null)
			return enumerator;

		return new List<TEntry>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public async IAsyncEnumerator<TEntry> GetAsyncEnumerator(CancellationToken cancellationToken = default)
	{
		foreach (var item in this)
		{
			cancellationToken.ThrowIfCancellationRequested();

			yield return item;

			await Task.CompletedTask;
		}
	}
	/*
	 * Here is potential performance penalty, as this will create a new IQueryable on each call. 
	 * However, since the underlying data source is likely in-memory, the performance impact may be negligible. 
	 * If performance becomes an issue, consider caching the IQueryable or implementing a more efficient way to handle LINQ queries.
	 * But the current problem is the All method combines entities from two caches, the scoped one and shared one.
	 * The problem is with the shared one because we are unaware of the changes so we can't make a snapshot and safely
	 * share it inside the scope.
	 */
	protected virtual IQueryable<TEntry> AsQueryable() => Context.All<TEntry>(Key).AsQueryable();
	public Type ElementType => typeof(TEntry);
	public Expression Expression => AsQueryable().Expression;
	public IQueryProvider Provider => AsQueryable().Provider;
}
