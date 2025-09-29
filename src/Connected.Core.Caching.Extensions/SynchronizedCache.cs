using Connected.Reflection;
using Connected.Threading;
using System.Collections.Immutable;

namespace Connected.Caching;

public abstract class SynchronizedCache<TEntry, TKey>(ICachingService cachingService, string key)
	: CacheContainer<TEntry, TKey>(cachingService, key), ISynchronizedCache<TEntry, TKey>
{
	static SynchronizedCache()
	{
		Initializers = [];
	}

	private static HashSet<string> Initializers { get; }
	private AsyncLockerSlim? Locker { get; set; } = new();

	protected bool Initialized
	{
		get => Initializers.Contains(Key);
		private set
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

	protected virtual async Task OnInvalidated(TKey id)
	{
		await Task.CompletedTask;
	}

	protected virtual async Task OnInitializing()
	{
		await Task.CompletedTask;
	}

	async Task ICachingDataProvider.Invalidate(object id)
	{
		var converted = Types.Convert<TKey>(id);

		if (converted is not null)
			await OnInvalidate(converted);

		await OnInvalidated(converted);
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

	public override async Task<IImmutableList<TEntry>> All()
	{
		await ((ICachingDataProvider)this).Initialize();

		return base.All().Result;
	}

	public override async Task<TEntry?> First()
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.First();
	}

	public override async Task<TEntry?> Get(Func<TEntry, bool> predicate)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Get(predicate);
	}

	public override async Task<TEntry?> Get(TKey id)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Get(id);
	}

	public override async Task<TEntry?> Get(TKey id, Func<IEntryOptions, Task<TEntry?>>? retrieve)
	{
		await ((ICachingDataProvider)this).Initialize();

		return await base.Get(id, retrieve);
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