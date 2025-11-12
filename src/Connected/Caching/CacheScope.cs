using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Caching;
internal sealed class CacheScope : ICache, IDisposable
{
	private bool _disposedValue;
	private readonly ConcurrentDictionary<string, Entries> _items = [];
	private readonly Task _scavenger;
	private readonly CancellationTokenSource _cancel = new();

	public CacheScope()
	{
		_scavenger = new Task(OnScaveging, _cancel.Token, TaskCreationOptions.LongRunning);
		_scavenger.Start();
	}

	public ConcurrentDictionary<string, Entries> Items => _items;
	public CancellationTokenSource Cancel => _cancel;

	private void OnScaveging()
	{
		var token = Cancel.Token;

		while (!token.IsCancellationRequested)
		{
			try
			{
				foreach (var i in Items)
					i.Value.Scave();

				var empties = Items.Where(f => f.Value.Count == 0).Select(f => f.Key);

				foreach (var i in empties)
					Items.TryRemove(i, out _);

				token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
			}
			catch { }
		}
	}

	public bool IsEmpty(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Any();

		return true;
	}

	public bool Exists(string key)
	{
		return Items.ContainsKey(key);
	}

	public IEnumerator<T>? GetEnumerator<T>(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.GetEnumerator<T>();

		return new List<T>().GetEnumerator();
	}

	public IImmutableList<T> All<T>(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.All<T>();

		return ImmutableList<T>.Empty;
	}

	public int Count(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Count;

		return 0;
	}

	public T? Get<T>(string key, Func<T, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(predicate) is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public async Task<T?> Get<T>(string key, Func<T, bool> predicate, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(predicate) is IEntry entry)
			return GetValue<T>(entry);

		if (retrieve is null)
			return default;

		var options = new CacheEntryOptions();

		if (retrieve is null)
			return default;

		T? instance = await retrieve(options);

		if (EqualityComparer<T>.Default.Equals(instance, default))
		{
			if (!options.AllowNull)
				return default;
		}

		if (options.Key is null)
		{
			if (instance is not null)
			{
				options.KeyProperty ??= CachingUtils.GetCacheKeyProperty(instance)?.Name;

				if (options.KeyProperty is not null)
				{
					var proposedKey = instance.GetType().GetProperty(options.KeyProperty)?.GetValue(instance)?.ToString();

					if (proposedKey is not null)
						options.Key = proposedKey;
				}
			}

			if (options.Key is null)
				throw new NullReferenceException(CacheStrings.ErrCacheKeyNull);
		}

		Set(key, options.Key, instance, options.Duration, options.SlidingExpiration);

		return instance;
	}

	public async Task<T?> Get<T>(string key, object id, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(ResolveId(id)) is IEntry entry)
			return GetValue<T>(entry);

		if (retrieve is null)
			return default;

		var options = new CacheEntryOptions
		{
			Key = id?.ToString() ?? throw new NullReferenceException(Exceptions.ErrCacheIdResolvedNull)
		};

		if (retrieve is null)
			return default;

		T? instance = await retrieve(options);

		if (EqualityComparer<T>.Default.Equals(instance, default))
		{
			if (!options.AllowNull)
				return default;
		}

		if (options.Key is null)
			throw new NullReferenceException(CacheStrings.ErrCacheIdNull);

		Set(key, options.Key, instance, options.Duration, options.SlidingExpiration);

		return instance;
	}

	public async Task Clear(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			value.Clear();

		await Task.CompletedTask;
	}

	public T? Get<T>(string key, object id)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(ResolveId(id)) is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public IEntry? Get(string key, object id)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Get(ResolveId(id));

		return default;
	}

	public T? Get<T>(string key, Func<dynamic, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(predicate) is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public T? First<T>(string key)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.First() is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public IImmutableList<T> Where<T>(string key, Func<T, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Where(predicate);

		return ImmutableList<T>.Empty;
	}

	public void CopyTo(string key, object id, IEntry instance)
	{
		if (!Items.TryGetValue(key, out Entries? value))
		{
			value = new Entries();

			if (!Items.TryAdd(key, value))
				return;
		}

		value.Set(ResolveId(id), instance.Instance, instance.Duration, instance.SlidingExpiration);
	}

	public T? Set<T>(string key, object id, T? instance)
	{
		return Set(key, id, instance, TimeSpan.Zero);
	}

	public T? Set<T>(string key, object id, T? instance, TimeSpan duration)
	{
		return Set(key, id, instance, duration, false);
	}

	public T? Set<T>(string key, object id, T? instance, TimeSpan duration, bool slidingExpiration)
	{
		if (!Items.TryGetValue(key, out Entries? value))
		{
			value = new Entries();

			if (!Items.TryAdd(key, value))
				return default;
		}

		value.Set(ResolveId(id), instance, duration, slidingExpiration);

		return instance;
	}

	public async Task Remove(string key, object id)
	{
		if (Items.TryGetValue(key, out Entries? value))
			value.Remove(ResolveId(id));

		await Task.CompletedTask;
	}

	public async Task<IImmutableList<string>> Remove<T>(string key, Func<T, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Remove(predicate);

		return await Task.FromResult(ImmutableList<string>.Empty);
	}

	public IImmutableList<string> Ids(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Keys;

		return ImmutableList<string>.Empty;
	}

	public IImmutableList<string> Keys()
	{
		return [.. Items.Keys];
	}

	public static string ResolveId(object id)
	{
		if (id is null)
			throw new NullReferenceException(CacheStrings.ErrCacheIdNull);

		return id.ToString() ?? throw new NullReferenceException(CacheStrings.ErrCacheIdNull);
	}

	private static T? GetValue<T>(IEntry entry)
	{
		if (entry is null || entry.Instance is null)
			return default;

		return (T)entry.Instance;
	}

	private void OnDisposing(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				Cancel.Cancel();

				foreach (var i in Items)
					i.Value.Clear();

				Items.Clear();

				if (_scavenger is not null)
				{
					_cancel.Cancel();

					if (_scavenger.IsCompleted)
						_scavenger.Dispose();
				}
			}

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		OnDisposing(true);
		GC.SuppressFinalize(this);
	}
}
