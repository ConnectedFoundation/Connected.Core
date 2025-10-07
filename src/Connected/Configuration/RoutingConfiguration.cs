using Microsoft.Extensions.Configuration;

namespace Connected.Configuration;

internal sealed class RoutingConfiguration : IRoutingConfiguration
{
	public RoutingConfiguration(IConfiguration configuration)
	{
		configuration.Bind(this);
	}

	public string? BaseUrl { get; init; }

	public string? RoutingServerUrl { get; init; }
}