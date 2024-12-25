using Connected.Configuration.Authentication;
using Connected.Configuration.Endpoints;

namespace Connected.Configuration;

public interface IConfigurationService
{
	IEndpointConfiguration Endpoint { get; }
	IAuthenticationConfiguration Authentication { get; }
	IStorageConfiguration Storage { get; }
	IRoutingConfiguration Routing { get; }
}
