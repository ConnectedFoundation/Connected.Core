using Connected.Reflection;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Caching;

internal abstract class Cache : ICache
{
	private bool _disposedValue;
	private readonly ConcurrentDictionary<string, Entries> _items;
	private readonly Task _scavenger;
	private readonly CancellationTokenSource _cancel = new();

	public Cache()
	{
		_scavenger = new Task(OnScaveging, _cancel.Token, TaskCreationOptions.LongRunning);
		_items = new ConcurrentDictionary<string, Entries>();

		_scavenger.Start();
	}

	private ConcurrentDictionary<string, Entries> Items => _items;
	private CancellationTokenSource Cancel => _cancel;

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

	public virtual bool IsEmpty(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Any();

		return true;
	}

	public virtual bool Exists(string key)
	{
		return Items.ContainsKey(key);
	}

	public void CreateKey(string key)
	{
		if (Exists(key))
			return;

		Items.TryAdd(key, new Entries());
	}

	public IEnumerator<T>? GetEnumerator<T>(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.GetEnumerator<T>();

		return new List<T>().GetEnumerator();
	}

	public virtual ImmutableList<T>? All<T>(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.All<T>();

		return default;
	}

	public int Count(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Count;

		return 0;
	}

	public virtual T? Get<T>(string key, Func<T, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(predicate) is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public virtual async Task<T?> Get<T>(string key, Func<T, bool> predicate, Func<IEntryOptions, Task<T?>>? retrieve)
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
					options.Key = instance.GetType().GetProperty(options.KeyProperty)?.GetValue(instance)?.ToString();
			}

			if (options.Key is null)
				throw new NullReferenceException(CacheStrings.ErrCacheKeyNull);
		}

		Set(key, options.Key, instance, options.Duration, options.SlidingExpiration);

		return instance;
	}

	public virtual async Task<T?> Get<T>(string key, object id, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(ResolveId(id)) is IEntry entry)
			return GetValue<T>(entry);

		if (retrieve is null)
			return default;

		var options = new CacheEntryOptions
		{
			Key = id?.ToString()
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

	internal void ClearCore(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			value.Clear();
	}

	public virtual async Task Clear(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			value.Clear();

		await Task.CompletedTask;
	}

	public virtual T? Get<T>(string key, object id)
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

	public virtual T? Get<T>(string key, Func<dynamic, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.Get(predicate) is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public virtual T? First<T>(string key)
	{
		if (Items.TryGetValue(key, out Entries? value) && value.First() is IEntry entry)
			return GetValue<T>(entry);

		return default;
	}

	public virtual ImmutableList<T>? Where<T>(string key, Func<T, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Where(predicate);

		return default;
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

	public virtual T? Set<T>(string key, object id, T? instance)
	{
		return Set(key, id, instance, TimeSpan.Zero);
	}

	public virtual T? Set<T>(string key, object id, T? instance, TimeSpan duration)
	{
		return Set(key, id, instance, duration, false);
	}

	public virtual T? Set<T>(string key, object id, T? instance, TimeSpan duration, bool slidingExpiration)
	{
		return Set(key, id, instance, duration, slidingExpiration, false);
	}

	public virtual T? Set<T>(string key, object id, T? instance, TimeSpan duration, bool slidingExpiration, bool performSynchronization)
	{
		if (!Items.TryGetValue(key, out Entries? value))
		{
			value = new Entries();

			if (!Items.TryAdd(key, value))
				return default;
		}

		value.Set(ResolveId(id), instance, duration, slidingExpiration);

		if (performSynchronization)
			OnSynchronize(new InvalidateCacheDto { Id = Convert.ToString(id), Key = key }).Wait();

		return instance;
	}

	protected internal virtual async Task OnSynchronize(InvalidateCacheDto e)
	{
		await Task.CompletedTask;
	}

	internal void RemoveCore(string key, object id)
	{
		if (Items.TryGetValue(key, out Entries? value))
			value.Remove(ResolveId(id));
	}

	public virtual async Task Remove(string key, object id)
	{
		await Remove(key, id, true);
	}

	private async Task Remove(string key, object id, bool removing)
	{
		if (Items.TryGetValue(key, out Entries? value))
			value.Remove(ResolveId(id));

		if (removing)
			await OnRemove(key, ResolveId(id));
	}

	protected virtual async Task OnRemove(string key, object id)
	{
		await Task.CompletedTask;
	}

	private void Clear()
	{
		foreach (var i in Items)
			i.Value.Clear();

		Items.Clear();
	}

	public virtual async Task<ImmutableList<string>?> Remove<T>(string key, Func<T, bool> predicate)
	{
		if (Items.TryGetValue(key, out Entries? value))
		{
			var result = value.Remove(predicate);

			if (result is not null && !result.IsEmpty)
				await OnRemove(key, result);
		}

		return default;
	}

	protected virtual async Task OnRemove(string key, ImmutableList<string> ids)
	{
		await Task.CompletedTask;
	}

	public ImmutableList<string>? Ids(string key)
	{
		if (Items.TryGetValue(key, out Entries? value))
			return value.Keys;

		return default;
	}

	public ImmutableList<string> Keys()
	{
		return [.. Items.Keys];
	}

	private static T? GetValue<T>(IEntry entry)
	{
		if (entry is null || entry.Instance is null)
			return default;

		return Types.Convert<T>(entry.Instance);
	}

	protected static string ResolveId(object id)
	{
		if (id is null)
			throw new NullReferenceException(CacheStrings.ErrCacheIdNull);

		return id.ToString() ?? throw new NullReferenceException(CacheStrings.ErrCacheIdNull);
	}

	protected virtual void OnDisposing(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				Cancel.Cancel();
				Clear();

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
