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
}
