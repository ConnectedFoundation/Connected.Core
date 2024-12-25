using System;

namespace Connected.Caching;
/// <summary>
/// Represents a single entry in the cache catalog.
/// </summary>
public interface IEntry : IDisposable
{
	/// <summary>
	/// The actual instance which is cached.
	/// </summary>
	object? Instance { get; }
	/// <summary>
	/// The id of the instance.
	/// </summary>
	string Id { get; }
	/// <summary>
	/// Determines if the entry has expired.
	/// </summary>
	bool Expired { get; }
	/// <summary>
	/// The amount of time the entry will be stored in a cache. 
	/// A TimeSpan.Zero represents a permanent entry with no expiration.
	/// </summary>
	TimeSpan Duration { get; }
	/// <summary>
	/// Determines if the duration can be extended if the entry has been requested.
	/// </summary>
	bool SlidingExpiration { get; }
}
