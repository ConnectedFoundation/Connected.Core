using System.Collections.Immutable;

namespace Connected.Caching;
/// <summary>
/// Represents the cache container.
/// </summary>
public interface ICache
	: IDisposable
{
	/// <summary>
	/// Returns all non expired items for the specified key.
	/// </summary>
	IImmutableList<T> All<T>(string key);
	/// <summary>
	/// Returns an item from the container if exists, otherwise null.
	/// </summary>
	/// <param name="key">The container in which the entry will be searched.</param>
	/// <param name="id">The id of the entry.</param>
	/// <param name="retrieve">The callback function to be called if the entry does not exist with a chance to
	/// load a missing entry from a storage. If this method returns a non null value it will be stored in a container.</param>
	/// <typeparam name="T">The type of the entry which will be returned.</typeparam>
	/// <returns>An instance of type T if found, null otherwise.</returns>
	Task<T?> Get<T>(string key, object id, Func<IEntryOptions, Task<T?>>? retrieve);
	/// <summary>
	/// Returns an item from the container if exists, otherwise null.
	/// </summary>
	/// <param name="key">The container in which the entry will be searched.</param>
	/// <param name="id">The id of the entry.</param>
	/// <typeparam name="T">The type of the entry which will be returned.</typeparam>
	/// <returns>An instance of type T if found, null otherwise.</returns>
	T? Get<T>(string key, object id);
	/// <summary>
	/// Returns an item from the container if exists, otherwise null.
	/// </summary>
	/// <param name="key">The container in which the entry will be searched.</param>
	/// <param name="id">The id of the entry.</param>
	/// <returns>An Entry if found, null otherwise.</returns>
	IEntry? Get(string key, object id);
	/// <summary>
	/// Returns an item from the container if exists, otherwise null.
	/// </summary>
	/// <param name="key">The container in which the entry will be searched.</param>
	/// <param name="predicate">The criteria used to search for the entry.</param>
	/// <param name="retrieve">The callback function to be called if the entry does not exist with a chance to
	/// load a missing entry from a storage. If this method returns a non null value it will be stored in a container.</param>
	/// <typeparam name="T">The type of the entry which will be returned.</typeparam>
	/// <returns>An instance of type T if found, null otherwise.</returns>
	Task<T?> Get<T>(string key, Func<T, bool> predicate, Func<IEntryOptions, Task<T?>>? retrieve);
	/// <summary>
	/// Returns an item from the container if exists, otherwise null.
	/// </summary>
	/// <param name="key">The container in which the entry will be searched.</param>
	/// <param name="predicate">The criteria used to search for the entry.</param>
	/// <typeparam name="T">The type of the entry which will be returned.</typeparam>
	/// <returns>An instance of type T if found, null otherwise.</returns>
	T? Get<T>(string key, Func<T, bool> predicate);
	/// <summary>
	/// Returns the first item a container if exists, otherwise null.
	/// </summary>
	/// <param name="key">The container in which the entry will be searched.</param>
	/// <typeparam name="T">The type of the entry which will be returned.</typeparam>
	/// <returns>An instance of type T if found, null otherwise.</returns>
	T? First<T>(string key);
	/// <summary>
	/// Returns an enumerator for the container.
	/// </summary>
	/// <param name="key">The container for which enumerator will be returned.</param>
	/// <typeparam name="T">The type of the entries stored in a container.</typeparam>
	/// <returns>An enumerator for the container if a container for the specified key exists, null otherwise.</returns>
	IEnumerator<T>? GetEnumerator<T>(string key);
	/// <summary>
	/// Returns items from the container for the specified criteria.
	/// </summary>
	/// <param name="key">The container in which the entries will be searched.</param>
	/// <param name="predicate">The criteria used to search for entries.</param>
	/// <typeparam name="T">The type of the entry which will be returned.</typeparam>
	/// <returns>A list of T instances which matches the criteria.</returns>
	IImmutableList<T> Where<T>(string key, Func<T, bool> predicate);
	/// <summary>
	/// Checks whether the specified container exists in the cache.
	/// </summary>
	/// <param name="key">The container whose existence to check.</param>
	/// <returns>True if container exists, false otherwise.</returns>
	bool Exists(string key);
	/// <summary>
	/// Checks whether the specified container is empty which means it contains no entries.
	/// </summary>
	/// <param name="key">The container to check.</param>
	/// <returns>True if container has no entries, false otherwise.</returns>
	bool IsEmpty(string key);
	/// <summary>
	/// Clears all entries in the container.
	/// </summary>
	/// <param name="key">The container to be cleared.</param>
	Task Clear(string key);
	/// <summary>
	/// Returns the number of entries in the specified container.
	/// </summary>
	/// <param name="key">The container to be counted.</param>
	int Count(string key);
	/// <summary>
	/// Adds a new entry or updates existing one if exists for the specified id with the default options.
	/// </summary>
	/// <param name="key">The container to which entry will be added or updated.</param>
	/// <param name="id">The id of the entry.</param>
	/// <param name="instance">The actual instance to be stored in the container.</param>
	T? Set<T>(string key, object id, T? instance);
	/// <summary>
	/// Adds a new entry or updates existing one if exists for the specified id with a specific duration.
	/// </summary>
	/// <param name="key">The container to which entry will be added or updated.</param>
	/// <param name="id">The id of the entry.</param>
	/// <param name="instance">The actual instance to be stored in the container.</param>
	/// <param name="duration">The duration until the entry expires.</param>
	T? Set<T>(string key, object id, T? instance, TimeSpan duration);
	/// <summary>
	/// Adds a new entry or updates existing one if exists for the specified id with a specific duration 
	/// and expiration behavior.
	/// </summary>
	/// <param name="key">The container to which entry will be added or updated.</param>
	/// <param name="id">The id of the entry.</param>
	/// <param name="instance">The actual instance to be stored in the container.</param>
	/// <param name="duration">The duration until the entry expires.</param>
	/// <param name="slidingExpiration">True to extend the expiration once the entry is requested.</param>
	T? Set<T>(string key, object id, T? instance, TimeSpan duration, bool slidingExpiration);
	/// <summary>
	/// Copies the external entry into the specified container. 
	/// </summary>
	/// <param name="key">The container to which entry will be copied.</param>
	/// <param name="id">The id of the entry.</param>
	/// <param name="entry">The actual to copy.</param>
	void CopyTo(string key, object id, IEntry entry);
	/// <summary>
	/// Removes the entries from the specified container that match the criteria.
	/// </summary>
	/// <param name="key">The container from which entries will be removed.</param>
	/// <param name="predicate">The criteria for removing entries.</param>
	/// <returns>The list of removed items.</returns>
	Task<IImmutableList<string>> Remove<T>(string key, Func<T, bool> predicate);
	/// <summary>
	/// Removes the entry from the specified container for the specified id.
	/// </summary>
	/// <param name="key">The container from which entries will be removed.</param>
	/// <param name="id">The id of the entry to be removed.</param>
	Task Remove(string key, object id);
	/// <summary>
	/// Returns ids of all entries for the specified container.
	/// </summary>
	/// <param name="key">The container from which ids will be returned.</param>
	/// <returns>The list of ids for the specified container.</returns>
	IImmutableList<string> Ids(string key);
	/// <summary>
	/// Returns keys of all containers in the cache.
	/// </summary>
	/// <returns>The list of keys containers.</returns>
	IImmutableList<string> Keys();
	/*
	 * Contract-only interface: concrete cache implementations provide storage and retrieval mechanics,
	 * expiration semantics (absolute/sliding), and synchronization/merge behavior across scopes.
	 */
}