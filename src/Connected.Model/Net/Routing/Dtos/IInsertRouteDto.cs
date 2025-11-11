using Connected.Services;

namespace Connected.Net.Routing.Dtos;

/// <summary>
/// Represents a data transfer object for inserting a new route.
/// </summary>
/// <remarks>
/// This interface defines the parameters required to create a new service route entry.
/// It includes the communication protocol, service identifier, and endpoint URL that
/// together define how clients can connect to a service. This DTO is used by routing
/// services to add new routes to the routing table, enabling dynamic service registration
/// and endpoint management in distributed systems. Routes can be added programmatically
/// or through administrative interfaces to configure service discovery and communication
/// paths.
/// </remarks>
public interface IInsertRouteDto
	: IDto
{
	/// <summary>
	/// Gets or sets the communication protocol for the route.
	/// </summary>
	/// <value>
	/// A <see cref="RouteProtocol"/> value specifying the protocol to use.
	/// </value>
	/// <remarks>
	/// The protocol determines how clients will communicate with the service, affecting
	/// client library selection and available features.
	/// </remarks>
	RouteProtocol Protocol { get; set; }

	/// <summary>
	/// Gets or sets the name of the service for this route.
	/// </summary>
	/// <value>
	/// A string representing the service identifier.
	/// </value>
	/// <remarks>
	/// The service name uniquely identifies which service this route provides access to.
	/// Clients use this name to look up routes when they need to communicate with the service.
	/// </remarks>
	string Service { get; set; }

	/// <summary>
	/// Gets or sets the endpoint URL for the route.
	/// </summary>
	/// <value>
	/// A string containing the full URL to the service endpoint.
	/// </value>
	/// <remarks>
	/// The URL specifies where the service can be reached, including protocol scheme,
	/// host, port, and any base path. This is the address clients will use to connect
	/// to the service.
	/// </remarks>
	string Url { get; set; }
}
