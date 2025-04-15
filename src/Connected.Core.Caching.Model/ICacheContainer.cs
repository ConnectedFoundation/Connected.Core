using System.Collections.Immutable;

namespace Connected.Caching;
/// <summary>
/// Represents the client of the cache providing access to the specific container.
/// </summary>
public interface ICacheContainer<TEntry, TKey> : IEnumerable<TEntry>, IDisposable
{
	/// <summary>
	/// The key of the container which manages this client.
	/// </summary>
	string Key { get; }
	/// <summary>
	/// The number of entries in the container.
	/// </summary>
	int Count { get; }

	Task Remove(TKey id);
	Task Remove(Func<TEntry, bool> predicate);
	Task<IImmutableList<TEntry>?> All();
	Task<TEntry?> Get(TKey id, Func<IEntryOptions, Task<TEntry?>>? retrieve);
	Task<TEntry?> Get(TKey id);
	Task<TEntry?> First();
	Task<TEntry?> Get(Func<TEntry, bool> predicate, Func<IEntryOptions, Task<TEntry?>>? retrieve);
	Task<TEntry?> Get(Func<TEntry, bool> predicate);
	void Set(TKey id, TEntry instance);
	void Set(TKey id, TEntry instance, TimeSpan duration);
	void Set(TKey id, TEntry instance, TimeSpan duration, bool slidingExpiration);
}
