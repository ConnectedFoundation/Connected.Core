using Connected.Services;

namespace Connected.Net.Routing.Dtos;

/// <summary>
/// Represents a data transfer object for selecting a route.
/// </summary>
/// <remarks>
/// This interface defines the query parameters used to look up a service route from the
/// routing table. By specifying the protocol and service name, clients can retrieve the
/// appropriate route configuration needed to connect to a service. This lookup mechanism
/// enables dynamic service discovery where endpoint addresses are resolved at runtime
/// rather than being hardcoded. The DTO supports scenarios where services may be accessible
/// through multiple protocols, requiring clients to specify which protocol they intend to use.
/// </remarks>
public interface ISelectRouteDto
	: IDto
{
	/// <summary>
	/// Gets or sets the communication protocol to filter routes by.
	/// </summary>
	/// <value>
	/// A <see cref="RouteProtocol"/> value specifying the desired protocol.
	/// </value>
	/// <remarks>
	/// The protocol filter ensures that only routes matching the specified protocol are
	/// returned, allowing clients to obtain endpoint information appropriate for their
	/// communication method.
	/// </remarks>
	RouteProtocol Protocol { get; set; }

	/// <summary>
	/// Gets or sets the name of the service to look up.
	/// </summary>
	/// <value>
	/// A string representing the service identifier.
	/// </value>
	/// <remarks>
	/// The service name identifies which service route to retrieve. Combined with the
	/// protocol, this uniquely identifies a specific route configuration.
	/// </remarks>
	string Service { get; set; }
}
