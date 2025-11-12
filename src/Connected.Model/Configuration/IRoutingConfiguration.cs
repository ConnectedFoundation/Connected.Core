namespace Connected.Configuration;

/// <summary>
/// Routing configuration used by clients and services to compose and discover endpoints.
/// </summary>
/// <remarks>
/// Provides base and server URLs used for request routing and service discovery.
/// </remarks>
public interface IRoutingConfiguration
{
	/// <summary>
	/// Gets the base URL used by clients to access services.
	/// </summary>
	string? BaseUrl { get; }
	/// <summary>
	/// Gets the routing server URL used for service discovery or reverse-proxy routing.
	/// </summary>
	string? RoutingServerUrl { get; }
}