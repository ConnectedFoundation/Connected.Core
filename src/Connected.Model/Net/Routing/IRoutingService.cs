using Connected.Annotations;
using Connected.Net.Routing.Dtos;
using Connected.Services;

namespace Connected.Net.Routing;

/// <summary>
/// Provides services for managing service routes and endpoint configuration.
/// </summary>
/// <remarks>
/// This service implements the core routing functionality for service discovery and
/// endpoint management in distributed systems. It maintains a routing table that maps
/// service names and protocols to endpoint URLs, enabling dynamic service discovery and
/// communication. The service supports CRUD operations on routes, allowing applications
/// to register services, update endpoint configurations, retrieve routes for client
/// connections, and remove obsolete routes. This forms the foundation for flexible
/// deployment topologies where services can be relocated, scaled, or reconfigured without
/// requiring client code changes. The routing service integrates with service registries,
/// configuration management, and load balancing infrastructure to provide robust service
/// connectivity.
/// </remarks>
[Service]
public interface IRoutingService
{
	/// <summary>
	/// Asynchronously selects a route based on protocol and service name.
	/// </summary>
	/// <param name="dto">The selection criteria containing the protocol and service name.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the matching route if found, or null if no route matches the criteria.
	/// </returns>
	/// <remarks>
	/// This method looks up a route from the routing table based on the specified protocol
	/// and service name. Clients use this operation to discover how to connect to a service
	/// before establishing communication. If no matching route exists, null is returned,
	/// indicating that the service is not currently reachable through the requested protocol.
	/// </remarks>
	Task<IRoute?> Select(ISelectRouteDto dto);

	/// <summary>
	/// Asynchronously updates an existing route.
	/// </summary>
	/// <param name="dto">The DTO containing the route identifier to update.</param>
	/// <returns>A task that represents the asynchronous update operation.</returns>
	/// <remarks>
	/// This method updates an existing route's configuration. While the interface signature
	/// suggests updating by primary key, implementations may support updating route properties
	/// such as URLs when endpoints change. This enables dynamic reconfiguration of service
	/// endpoints without requiring application restarts.
	/// </remarks>
	Task Update(IPrimaryKeyDto<Guid> dto);

	/// <summary>
	/// Asynchronously inserts a new route into the routing table.
	/// </summary>
	/// <param name="dto">The route information including protocol, service name, and URL.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the unique identifier of the newly created route.
	/// </returns>
	/// <remarks>
	/// This method adds a new route to the routing table, making the service discoverable
	/// by clients. This is typically called during service registration or deployment to
	/// advertise service endpoints. The returned identifier can be used for subsequent
	/// update or delete operations on the route.
	/// </remarks>
	Task<Guid> Insert(IInsertRouteDto dto);

	/// <summary>
	/// Asynchronously deletes a route from the routing table.
	/// </summary>
	/// <param name="dto">The DTO containing the route identifier to delete.</param>
	/// <returns>A task that represents the asynchronous delete operation.</returns>
	/// <remarks>
	/// This method removes a route from the routing table, making the service endpoint
	/// no longer discoverable through the routing service. This is typically called during
	/// service deregistration or when endpoints are being retired. After deletion, clients
	/// will no longer be able to discover or connect to the service through this route.
	/// </remarks>
	Task Delete(IPrimaryKeyDto<Guid> dto);
}
