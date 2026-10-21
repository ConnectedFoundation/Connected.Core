using Connected.Configuration;
using Connected.Configuration.Authentication;
using Connected.Services;
using Connected.Storage.Containers;
using System.Collections.Immutable;

namespace Connected.Storage.Tests;

/// <summary>
/// Configuration holding only a default connection string, for providers under test.
/// </summary>
internal sealed class TestConfiguration(string connectionString)
	: IConfigurationService, IStorageConfiguration, IDatabaseConfiguration
{
	/// <inheritdoc/>
	public IAuthenticationConfiguration Authentication => throw new NotSupportedException();

	/// <inheritdoc/>
	public IStorageConfiguration Storage => this;

	/// <inheritdoc/>
	public IRoutingConfiguration Routing => throw new NotSupportedException();

	/// <inheritdoc/>
	public IDatabaseConfiguration Databases => this;

	/// <inheritdoc/>
	public string? DefaultConnectionString => connectionString;

	/// <inheritdoc/>
	public IImmutableList<string> Shards => [];

	/// <inheritdoc/>
	public IImmutableDictionary<string, string> Connections => ImmutableDictionary<string, string>.Empty;

	/// <summary>Returns the connection string an environment variable holds, or marks the test inconclusive.</summary>
	/// <param name="variable">The environment variable.</param>
	/// <returns>The connection string.</returns>
	public static string From(string variable)
	{
		var value = Environment.GetEnvironmentVariable(variable);

		if (string.IsNullOrEmpty(value))
			Assert.Inconclusive($"Set {variable} to the connection string of a server the tests may create and drop databases on.");

		return value!;
	}

	/// <summary>Names a container.</summary>
	/// <param name="name">The container's name.</param>
	/// <returns>The DTO.</returns>
	public static IStorageContainerDto Container(string name) => new ContainerDto { Name = name };

	private sealed class ContainerDto
		: IStorageContainerDto
	{
		public required string Name { get; set; }
	}
}
