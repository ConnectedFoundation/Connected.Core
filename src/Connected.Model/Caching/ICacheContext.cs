using System.Collections.Immutable;

namespace Connected.Caching;
/// <summary>
/// Represents a scoped cache.
/// </summary>
/// <remarks>
/// A scoped cache acts as an intermediate layer between the client and a shared cache. The entries
/// are stored in the context cache until the context in committed. At this point, the changes are flushed
/// to the shared cache.
/// </remarks>
public interface ICacheContext
	: ICache
{
	/// <summary>
	/// Merges the changes with the shared cache.
	/// </summary>
	void Flush();
	/// <summary>
	/// Returns the keys owned by this context (not yet flushed).
	/// </summary>
	IImmutableList<string> OwnKeys();
	/// <summary>
	/// Returns the entry ids owned by this context for the specified container key.
	/// </summary>
	/// <param name="key">The container key.</param>
	IImmutableList<string> OwnIds(string key);
	/*
	 * Context caches accumulate transient mutations allowing commit-style merge semantics with a shared cache.
	 */
}