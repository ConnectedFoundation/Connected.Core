using System.Collections.Immutable;

namespace Connected.Configuration;

/// <summary>
/// Configuration for database connectivity and sharding.
/// </summary>
/// <remarks>
/// Exposes the default connection string and optional sharding configuration, including a list of shard names
/// and a mapping of logical names to connection strings.
/// </remarks>
public interface IDatabaseConfiguration
{
	/// <summary>
	/// Gets the default connection string used when no specific shard mapping applies.
	/// </summary>
	string? DefaultConnectionString { get; }

	/// <summary>
	/// Gets the list of shard identifiers.
	/// </summary>
	IImmutableList<string> Shards { get; }

	/// <summary>
	/// Gets the mapping of logical connection names to connection strings.
	/// </summary>
	IImmutableDictionary<string, string> Connections { get; }
}
