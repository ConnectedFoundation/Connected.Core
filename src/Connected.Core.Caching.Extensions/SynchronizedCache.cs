using Connected.Threading;

namespace Connected.Caching;

public abstract class SynchronizedCache<TEntry, TKey> : CacheContainer<TEntry, TKey>, ISynchronizedCache<TEntry, TKey> where TEntry : class
{
	static SynchronizedCache()
	{
		Initializers = new();
	}

	protected SynchronizedCache(ICachingService cachingService, string key) : base(cachingService, key)
	{
		Locker = new();
	}

	private static HashSet<string> Initializers { get; }
	private AsyncLockerSlim? Locker { get; set; }

	private bool Initialized
	{
		get => Initializers.Contains(Key);
		set
		{
			if (Initializers.Contains(Key))
				return;

			lock (Initializers)
			{
				if (Initializers.Contains(Key))
					return;

				Initializers.Add(Key);
			}
		}
	}

	protected virtual async Task OnInvalidate(TKey id)
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnInitializing()
	{
		await Task.CompletedTask;
	}

	async Task ICachingDataProvider.Invalidate(object id)
	{
		var converted = (TKey?)Convert.ChangeType(id, typeof(TKey));

		if (converted is not null)
			await OnInvalidate(converted);
	}

	async Task ICachingDataProvider.Initialize()
	{
		if (Initialized || IsDisposed || Locker is null)
			return;

		await Locker.LockAsync(async () =>
		{
			if (Initialized || IsDisposed)
				return;

			await OnInitializing();

			Initialized = true;
		});

		if (Initialized)
			await OnInitialized();
	}

	protected virtual async Task OnInitialized()
	{
		await Task.CompletedTask;
	}

	protected override async Task<ImmutableList<TEntry>?> All()
	{
		await ((ICachingDataProvider)this).Initialize();

		return base.All().Result;
	}

	protected override async Task<TEntry?> First()
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.First();
	}

	protected override async Task<TEntry?> Get(Func<TEntry, bool> predicate)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Get(predicate);
	}

	protected override async Task<TEntry?> Get(TKey id)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Get(id);
	}

	protected override async Task<TEntry?> Get(TKey id, Func<IEntryOptions, Task<TEntry?>>? retrieve)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Get(id, retrieve);
	}

	protected override async Task<ImmutableList<TEntry>?> Where(Func<TEntry, bool> predicate)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Where(predicate);
	}

	protected override void OnDisposing()
	{
		if (Locker is not null)
		{
			Locker?.Dispose();
			Locker = null;
		}
	}

	public override IEnumerator<TEntry> GetEnumerator()
	{
		((ICachingDataProvider)this).Initialize().Wait();

		return base.GetEnumerator();
	}
}