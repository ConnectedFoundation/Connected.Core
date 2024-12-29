using Microsoft.Extensions.Configuration;

namespace Connected.Configuration;

internal sealed class RoutingConfiguration : IRoutingConfiguration
{
	public RoutingConfiguration(IConfiguration configuration)
	{
		BaseUrl = string.Empty;

		configuration.Bind(this);
	}

	public string BaseUrl { get; }
}