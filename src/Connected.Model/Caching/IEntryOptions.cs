using System;

namespace Connected.Caching;
/// <summary>
/// The options related to the storing behavior ot the cache entry.
/// </summary>
public interface IEntryOptions
{
	/// <summary>
	/// Specifies the key of the entry to be used.
	/// </summary>
	string Key { get; set; }
	/// <summary>
	/// Specifies the property to be used when retrieving the Key value.
	/// </summary>
	string? KeyProperty { get; set; }
	/// <summary>
	/// Specifies the duration until the entry expires and is automatically
	/// removed from the cache. 
	/// </summary>
	TimeSpan Duration { get; set; }
	/// <summary>
	/// Specifies if the entry can extend the expiration once requested from the container.
	/// </summary>
	bool SlidingExpiration { get; set; }
	/// <summary>
	/// Specifies wether the entry can store null value.
	/// </summary>
	bool AllowNull { get; set; }
}