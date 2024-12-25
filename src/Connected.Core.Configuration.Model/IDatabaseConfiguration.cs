using System.Collections.Immutable;

namespace Connected.Configuration;

public interface IDatabaseConfiguration
{
	string? DefaultConnectionString { get; }

	ImmutableList<string> Shards { get; }
	ImmutableDictionary<string, string> Connections { get; }
}
