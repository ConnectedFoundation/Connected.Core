using System.Collections.Immutable;

namespace Connected.Caching;
/// <summary>
/// Represents the client of the cache providing access to the specific container.
/// </summary>
public interface ICacheContainer<TEntry, TKey>
	: IEnumerable<TEntry>, IDisposable
{
	/// <summary>
	/// The key of the container which manages this client.
	/// </summary>
	string Key { get; }
	/// <summary>
	/// The number of entries in the container.
	/// </summary>
	int Count { get; }
	/// <summary>
	/// Gets all keys for entries in this container.
	/// </summary>
	IImmutableList<string> Keys { get; }
	/// <summary>
	/// Removes an entry by id.
	/// </summary>
	/// <param name="id">The entry identifier.</param>
	Task Remove(TKey id);
	/// <summary>
	/// Removes entries matching the predicate.
	/// </summary>
	/// <param name="predicate">The predicate used for selection.</param>
	Task Remove(Func<TEntry, bool> predicate);
	/// <summary>
	/// Returns all entries.
	/// </summary>
	Task<IImmutableList<TEntry>> All();
	/// <summary>
	/// Gets or loads an entry by id, using retrieve callback when needed.
	/// </summary>
	Task<TEntry?> Get(TKey id, Func<IEntryOptions, Task<TEntry?>>? retrieve);
	/// <summary>
	/// Gets an entry by id.
	/// </summary>
	Task<TEntry?> Get(TKey id);
	/// <summary>
	/// Returns the first entry if present.
	/// </summary>
	Task<TEntry?> First();
	/// <summary>
	/// Gets or loads an entry using predicate, with a retrieve callback when needed.
	/// </summary>
	Task<TEntry?> Get(Func<TEntry, bool> predicate, Func<IEntryOptions, Task<TEntry?>>? retrieve);
	/// <summary>
	/// Gets an entry by predicate.
	/// </summary>
	Task<TEntry?> Get(Func<TEntry, bool> predicate);
	/// <summary>
	/// Stores a value with default options.
	/// </summary>
	void Set(TKey id, TEntry instance);
	/// <summary>
	/// Stores a value with an absolute expiration duration.
	/// </summary>
	void Set(TKey id, TEntry instance, TimeSpan duration);
	/// <summary>
	/// Stores a value with an absolute expiration and sliding expiration flag.
	/// </summary>
	void Set(TKey id, TEntry instance, TimeSpan duration, bool slidingExpiration);
	/*
	 * Client-style container interface around a specific key compartment in the cache, exposing
	 * common operations (get/set/remove) with optional lazy retrieval semantics.
	 */
}