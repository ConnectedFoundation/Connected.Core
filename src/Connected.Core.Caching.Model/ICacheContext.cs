namespace Connected.Caching;
/// <summary>
/// Represents a scoped cache.
/// </summary>
/// <remarks>
/// A scoped cache acts as an intermediate layer between the client and a shared cache. The entries
/// are stored in the context cache until the context in committed. At this point, the changes are flushed
/// to the shared cache.
/// </remarks>
public interface ICacheContext : ICache
{
	/// <summary>
	/// Merges the changes with the shared cache.
	/// </summary>
	void Flush();
}
