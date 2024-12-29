using Connected.Annotations;
using Connected.Configuration.Authentication;

namespace Connected.Configuration;

[Service(ServiceRegistrationScope.Singleton)]
public interface IConfigurationService
{
	IAuthenticationConfiguration Authentication { get; }
	IStorageConfiguration Storage { get; }
	IRoutingConfiguration Routing { get; }
}
