namespace Connected.Caching;
/// <summary>
/// In-process cache interface combining general cache operations with merge semantics.
/// </summary>
public interface IInProcessCache
	: ICache
{
	/// <summary>
	/// Merges a scoped context into the in-process cache.
	/// </summary>
	/// <param name="cache">Scoped cache context.</param>
	void Merge(ICacheContext cache);
	/*
	 * Provides in-memory caching plus context consolidation functionality.
	 */
}
