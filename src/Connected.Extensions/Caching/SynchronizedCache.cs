using Connected.Reflection;
using Connected.Threading;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

namespace Connected.Caching;

public abstract class SynchronizedCache<TEntry, TKey>(ICachingService cachingService, string key)
	: CacheContainer<TEntry, TKey>(cachingService, key), ISynchronizedCache<TEntry, TKey>
{
	static SynchronizedCache()
	{
		Initializers = [];
		Lockers = [];
	}

	private static Lock Lock { get; } = new();
	private static HashSet<string> Initializers { get; }
	/*
	 * Containers are resolved per scope but both the initialization state and the entries they guard
	 * are shared, so the locker must be shared as well. A per instance locker would let an Initialize
	 * running in one scope hydrate on top of a Reset performed in another, leaving the container
	 * marked as initialized while holding entries the reset was meant to discard.
	 */
	private static ConcurrentDictionary<string, AsyncLockerSlim> Lockers { get; }
	private AsyncLockerSlim Locker => Lockers.GetOrAdd(Key, _ => new AsyncLockerSlim());

	protected bool Initialized
	{
		get => IsInitialized(Key);
		private set
		{
			if (IsInitialized(Key))
				return;

			lock (Lock)
			{
				Initializers.Add(Key);
			}
		}
	}

	/// <summary>
	/// Drops all entries and marks the container as not initialized so the next access hydrates
	/// it again from the storage.
	/// </summary>
	/// <remarks>
	/// Runs under the same locker as initialization, so a concurrent <c>Initialize</c> either
	/// completes before the container is cleared or hydrates after it, never in between.
	/// </remarks>
	protected async Task Reset()
	{
		if (IsDisposed)
			return;

		await Locker.LockAsync(async () =>
		{
			if (IsDisposed)
				return;

			/*
			 * Entries first, initialization flag second. Readers which don't take the locker because
			 * they see an initialized container get a transient miss at worst, whereas the opposite
			 * order would leave the container permanently marked as initialized but empty.
			 */
			await Clear();

			lock (Lock)
			{
				Initializers.Remove(Key);
			}
		});
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
		{
			await OnInvalidate(converted);
			await OnInvalidated(converted);
		}
	}

	async Task ICachingDataProvider.Reset()
	{
		await Reset();
	}

	async Task ICachingDataProvider.Initialize()
	{
		if (Initialized || IsDisposed)
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

		return await base.All();
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

	public override IEnumerator<TEntry> GetEnumerator()
	{
		((ICachingDataProvider)this).Initialize().Wait();

		return base.GetEnumerator();
	}

	private bool IsInitialized(string key)
	{
		lock (Lock)
		{
			return Initializers.Contains(key);
		}
	}
}