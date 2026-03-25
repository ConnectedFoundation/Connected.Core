namespace Connected.Caching;

/// <summary>
/// Default implementation of cache entry options that configures storage behavior for cache entries.
/// </summary>
/// <remarks>
/// Provides default values for duration (5 minutes), sliding expiration (enabled), 
/// key ("."), and merge behavior (Merge).
/// </remarks>
public class CacheEntryOptions
	: IEntryOptions
{
	/// <inheritdoc/>
	public string Key { get; set; }

	/// <inheritdoc/>
	public string? KeyProperty { get; set; }

	/// <inheritdoc/>
	public TimeSpan Duration { get; set; }

	/// <inheritdoc/>
	public bool SlidingExpiration { get; set; }

	/// <inheritdoc/>
	public bool AllowNull { get; set; }

	/// <inheritdoc/>
	public CacheEntryMergeBehavior Merge { get; set; } = CacheEntryMergeBehavior.Merge;

	/// <summary>
	/// Initializes a new instance of the <see cref="CacheEntryOptions"/> class with default values.
	/// </summary>
	/// <remarks>
	/// Sets the following defaults:
	/// Duration is set to 5 minutes,
	/// SlidingExpiration is enabled,
	/// Key is set to ".",
	/// Merge behavior is set to Merge (via field initializer).
	/// </remarks>
	public CacheEntryOptions()
	{
		/*
		 * Initialize default cache entry behavior with common settings.
		 * Duration of 5 minutes provides reasonable caching without stale data concerns.
		 * Sliding expiration keeps frequently accessed entries alive.
		 * Default key "." serves as a placeholder when key is derived from KeyProperty.
		 */
		Duration = TimeSpan.FromMinutes(5);
		SlidingExpiration = true;
		Key = ".";
	}
}
