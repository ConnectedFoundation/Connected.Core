namespace Connected.Caching;
/// <summary>
/// A synchronized cache combining data provisioning with container-style access.
/// </summary>
/// <remarks>
/// Provides both hydration (via <see cref="ICachingDataProvider"/>) and container operations
/// (via <see cref="ICacheContainer{TEntry, TKey}"/>) for a keyed entry set.
/// </remarks>
/// <typeparam name="TEntry">Entry type.</typeparam>
/// <typeparam name="TKey">Key type.</typeparam>
public interface ISynchronizedCache<TEntry, TKey>
	: ICachingDataProvider, ICacheContainer<TEntry, TKey>
{
}

