using Connected.Configuration.Authentication;
using Microsoft.Extensions.Configuration;

namespace Connected.Configuration;

internal sealed class ConfigurationService(IConfiguration configuration) : IConfigurationService
{
	public IAuthenticationConfiguration Authentication { get; } = new AuthenticationConfiguration(configuration.GetSection("authentication"));
	public IStorageConfiguration Storage { get; } = new StorageConfiguration(configuration.GetSection("storage"));
	public IRoutingConfiguration Routing { get; } = new RoutingConfiguration(configuration.GetSection("routing"));
}
