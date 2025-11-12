namespace Connected.Caching;
/// <summary>
/// Entity-focused cache abstraction supporting synchronized access and refresh operations.
/// </summary>
/// <typeparam name="TEntry">Entry type stored.</typeparam>
/// <typeparam name="TKey">Entry key type.</typeparam>
public interface IEntityCache<TEntry, TKey>
	: ISynchronizedCache<TEntry, TKey>
{
	/// <summary>
	/// Refreshes the cached representation for the specified id (reload from backing store).
	/// </summary>
	/// <param name="id">Entry identifier.</param>
	Task Refresh(TKey id);
	/*
	 * Provides targeted refresh semantics beyond base synchronization.
	 */
}
