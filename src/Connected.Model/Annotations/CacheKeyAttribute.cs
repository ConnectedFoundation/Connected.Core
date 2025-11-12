namespace Connected.Annotations;

/// <summary>
/// Marks a property as a cache key so caching layers can identify the value to use
/// when storing and retrieving entries.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CacheKeyAttribute
	: Attribute
{
	/*
	 * Marker attribute with no state; presence indicates that the decorated property
	 * participates as the cache key for the owning type.
	 */
}
