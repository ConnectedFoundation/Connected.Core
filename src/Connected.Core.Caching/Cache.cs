using System.Collections.Immutable;

namespace Connected.Caching;

internal abstract class Cache : ICache
{
	private static readonly CacheScope _scope = new();

	public virtual bool IsEmpty(string key)
	{
		return _scope.IsEmpty(key);
	}

	public virtual bool Exists(string key)
	{
		return _scope.Exists(key);
	}

	public IEnumerator<T>? GetEnumerator<T>(string key)
	{
		return _scope.GetEnumerator<T>(key);
	}

	public virtual ImmutableList<T>? All<T>(string key)
	{
		return _scope.All<T>(key);
	}

	public int Count(string key)
	{
		return _scope.Count(key);
	}

	public virtual T? Get<T>(string key, Func<T, bool> predicate)
	{
		return _scope.Get(key, predicate);
	}

	public virtual async Task<T?> Get<T>(string key, Func<T, bool> predicate, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		return await _scope.Get(key, predicate, retrieve);
	}

	public virtual async Task<T?> Get<T>(string key, object id, Func<IEntryOptions, Task<T?>>? retrieve)
	{
		return await Get(key, id, retrieve);
	}

	public virtual async Task Clear(string key)
	{
		await _scope.Clear(key);
	}

	public virtual T? Get<T>(string key, object id)
	{
		return _scope.Get<T>(key, id);
	}

	public IEntry? Get(string key, object id)
	{
		return _scope.Get(key, id);
	}

	public virtual T? Get<T>(string key, Func<dynamic, bool> predicate)
	{
		return _scope.Get(key, predicate);
	}

	public virtual T? First<T>(string key)
	{
		return _scope.First<T>(key);
	}

	public virtual ImmutableList<T>? Where<T>(string key, Func<T, bool> predicate)
	{
		return _scope.Where(key, predicate);
	}

	public void CopyTo(string key, object id, IEntry instance)
	{
		_scope.CopyTo(key, id, instance);
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
		var result = _scope.Set(key, id, instance, duration, slidingExpiration);

		if (performSynchronization)
			OnSynchronize(new InvalidateCacheDto { Id = Convert.ToString(id), Key = key }).Wait();

		return result;
	}

	protected internal virtual async Task OnSynchronize(InvalidateCacheDto e)
	{
		await Task.CompletedTask;
	}

	public virtual async Task Remove(string key, object id)
	{
		await Remove(key, id, true);
	}

	private async Task Remove(string key, object id, bool removing)
	{
		await _scope.Remove(key, id);

		if (removing)
			await OnRemove(key, ResolveId(id));
	}

	protected virtual async Task OnRemove(string key, object id)
	{
		await Task.CompletedTask;
	}

	public virtual async Task<ImmutableList<string>?> Remove<T>(string key, Func<T, bool> predicate)
	{
		var items = await _scope.Remove(key, predicate);

		if (items is not null && !items.IsEmpty)
			await OnRemove(key, items);

		return items;
	}

	protected virtual async Task OnRemove(string key, ImmutableList<string> ids)
	{
		await Task.CompletedTask;
	}

	public ImmutableList<string>? Ids(string key)
	{
		return _scope.Ids(key);
	}

	public ImmutableList<string> Keys()
	{
		return [.. _scope.Items.Keys];
	}

	protected static string ResolveId(object id)
	{
		return CacheScope.ResolveId(id);
	}

	protected virtual void OnDisposing(bool disposing)
	{
		_scope.Dispose();
	}

	public void Dispose()
	{
		OnDisposing(true);
		GC.SuppressFinalize(this);
	}
}
