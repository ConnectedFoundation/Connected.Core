using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;

namespace Connected.Configuration;

internal class DatabaseConfiguration : IDatabaseConfiguration
{
	private readonly List<string> _shards;
	private readonly Dictionary<string, string> _connections;

	public DatabaseConfiguration(IConfiguration configuration)
	{
		_shards = [];
		_connections = [];

		DefaultConnectionString = string.Empty;

		configuration.Bind(this);
		configuration.Bind("shards", _shards);
		configuration.Bind("connections", _connections);
	}

	public string? DefaultConnectionString { get; init; }

	public IImmutableList<string> Shards => [.. _shards];
	public IImmutableDictionary<string, string> Connections => _connections.ToImmutableDictionary();
}
