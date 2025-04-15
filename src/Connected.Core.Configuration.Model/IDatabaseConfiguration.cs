using System.Collections.Immutable;

namespace Connected.Configuration;

public interface IDatabaseConfiguration
{
	string? DefaultConnectionString { get; }

	IImmutableList<string> Shards { get; }
	IImmutableDictionary<string, string> Connections { get; }
}
