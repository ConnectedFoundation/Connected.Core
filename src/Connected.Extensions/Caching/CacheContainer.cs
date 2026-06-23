using Connected.Reflection;
using Microsoft.AspNetCore.OutputCaching;
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

		_provider = new EnumerableQueryProvider(this, Context, Key);
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
		var enumerator = Provider.Execute<IEnumerable<TEntry>>(Expression).GetEnumerator();

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
		foreach (var item in this.Provider.Execute<IEnumerable<TEntry>>(Expression))
		{
			cancellationToken.ThrowIfCancellationRequested();

			yield return item;

			await Task.CompletedTask;
		}
	}
	public Type ElementType => typeof(TEntry);
	public Expression Expression => Expression.Constant(this);
	public IQueryProvider Provider => _provider;

	private EnumerableQueryProvider _provider;

	private sealed class EnumerableQueryProvider(ICacheContainer<TEntry, TKey> cache, ICacheContext context, string key) : IQueryProvider
	{
		public IQueryable CreateQuery(Expression expression)
			=> CreateQuery<TEntry>(expression);

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
			=> new EnumerableQueryable<TElement>(expression, this);

		public object? Execute(Expression expression)
		{
			if (cache is ICachingDataProvider provider)
				provider.Initialize().Wait();
			var snapshot = context.All<TEntry>(key).AsQueryable();
			var rewritten = new StaleSourceReplacer(snapshot).Visit(expression);
			return snapshot.Provider.Execute(rewritten);
		}

		public TResult Execute<TResult>(Expression expression)
		{
			if (cache is ICachingDataProvider provider)
				provider.Initialize().Wait();
			var snapshot = context.All<TEntry>(key).AsQueryable();
			var rewritten = new StaleSourceReplacer(snapshot).Visit(expression);
			return snapshot.Provider.Execute<TResult>(rewritten);
		}

		private sealed class StaleSourceReplacer(IQueryable<TEntry> snapshot) : ExpressionVisitor
		{
			protected override Expression VisitConstant(ConstantExpression node)
				=> node.Value is IQueryable<TEntry> ? snapshot.Expression : base.VisitConstant(node);
		}
	}

	private sealed class EnumerableQueryable<T>(Expression expression, IQueryProvider provider) : IQueryable<T>
	{
		public Type ElementType => typeof(T);
		public Expression Expression => expression;
		public IQueryProvider Provider => provider;
		public IEnumerator<T> GetEnumerator() => provider.Execute<IEnumerable<T>>(expression).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
