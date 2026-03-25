namespace Connected.Caching;

/// <summary>
/// Defines the behavior when a DI scope containing cache entries is flushed (disposed but not rolled back).
/// </summary>
/// <remarks>
/// Cache entries are first stored in a context isolated from other DI scopes. When the scope is disposed,
/// this behavior determines whether entries are persisted to the shared cache or discarded.
/// </remarks>
public enum CacheEntryMergeBehavior
{
	/// <summary>
	/// Merge the cache entries from the disposed scope with the shared cache.
	/// </summary>
	/// <remarks>
	/// Entries will be persisted to the shared cache and remain available beyond the scope lifetime.
	/// </remarks>
	Merge = 0,

	/// <summary>
	/// Discard the cache entries from the disposed scope.
	/// </summary>
	/// <remarks>
	/// Entries will only live as long as the DI scope lives and will not be persisted to the shared cache.
	/// </remarks>
	Discard = 1
}
/// <summary>
/// The options related to the storing behavior of the cache entry.
/// </summary>
/// <remarks>
/// These options control how cache entries are stored, retrieved, and expired within the isolated cache context
/// and how they are persisted to the shared cache when the DI scope is disposed.
/// </remarks>
public interface IEntryOptions
{
	/// <summary>
	/// Gets or sets the key of the entry to be used for cache storage and retrieval.
	/// </summary>
	string Key { get; set; }

	/// <summary>
	/// Gets or sets the property name to be used when retrieving the key value from the cached object.
	/// </summary>
	/// <remarks>
	/// If specified, the key will be extracted from this property of the cached object instead of using the Key property directly.
	/// </remarks>
	string? KeyProperty { get; set; }

	/// <summary>
	/// Gets or sets the duration until the entry expires and is automatically removed from the cache.
	/// </summary>
	TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the entry can extend the expiration time when accessed from the container.
	/// </summary>
	/// <remarks>
	/// When set to true, each access to the cache entry resets the expiration timer based on the Duration value.
	/// </remarks>
	bool SlidingExpiration { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the entry can store null values.
	/// </summary>
	bool AllowNull { get; set; }

	/// <summary>
	/// Gets or sets the merge behavior that determines whether the entry is persisted to the shared cache when the DI scope is disposed.
	/// </summary>
	/// <remarks>
	/// When the containing scope is flushed, this setting controls whether the entry is merged with the shared cache
	/// or discarded, living only as long as the scope.
	/// </remarks>
	CacheEntryMergeBehavior Merge { get; set; }
}