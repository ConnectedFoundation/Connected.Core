using Connected.Annotations;
using Connected.Configuration.Authentication;

namespace Connected.Configuration;
/// <summary>
/// Provides access to application configuration sections used across the runtime.
/// </summary>
/// <remarks>
/// Implementations of this service expose configuration for authentication, storage
/// and routing. The interface is registered as a singleton so implementations should
/// be safe for concurrent use and not contain per-request state.
/// </remarks>
[Service(ServiceRegistrationScope.Singleton)]
public interface IConfigurationService
{
	/// <summary>
	/// Gets the authentication-related configuration values such as JWT settings.
	/// </summary>
	IAuthenticationConfiguration Authentication { get; }

	/// <summary>
	/// Gets configuration values related to storage and database connections.
	/// </summary>
	IStorageConfiguration Storage { get; }

	/// <summary>
	/// Gets routing configuration used by network and service discovery components.
	/// </summary>
	IRoutingConfiguration Routing { get; }
}
