using Connected.Annotations;

namespace Connected.Caching;
/// <summary>
/// Represents the service providing caching capabilities.
/// </summary>
[Service]
public interface ICachingService
	: ICache
{
	/// <summary>
	/// Merges the entries from the passed cache into the current cache.
	/// </summary>
	/// <remarks>
	/// This method is called from the context cache once the commit is performed.
	/// </remarks>
	void Merge(ICacheContext cache);
	/// <summary>
	/// Creates a new scoped cache context for accumulating mutations before merge.
	/// </summary>
	ICacheContext CreateContext();
	/*
	 * Service-level facade for cache operations plus context creation/merge semantics.
	 */
}