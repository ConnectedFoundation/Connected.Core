using Connected.Entities;

namespace Connected.Net.Routing;

/// <summary>
/// Defines the communication protocol for service routes.
/// </summary>
/// <remarks>
/// This enumeration specifies which network protocol should be used for communicating
/// with a service. Different protocols have different characteristics regarding performance,
/// compatibility, and features. HTTP is widely supported and works well for REST APIs,
/// while gRPC provides better performance for high-throughput scenarios and supports
/// features like bidirectional streaming. The protocol selection affects how clients
/// connect to services and what features are available for communication.
/// </remarks>
public enum RouteProtocol
{
	/// <summary>
	/// Indicates the route uses HTTP protocol.
	/// </summary>
	/// <remarks>
	/// HTTP protocol is commonly used for REST APIs and web services, providing broad
	/// compatibility and ease of debugging.
	/// </remarks>
	Http = 1,

	/// <summary>
	/// Indicates the route uses gRPC protocol.
	/// </summary>
	/// <remarks>
	/// gRPC protocol provides high-performance RPC communication with features like
	/// bidirectional streaming, built-in load balancing, and efficient binary serialization.
	/// </remarks>
	Grpc = 2
}

/// <summary>
/// Represents a service route configuration.
/// </summary>
/// <remarks>
/// This interface defines the routing information required to connect to a service,
/// including the service identifier, communication protocol, and endpoint URL. Routes
/// enable service discovery and dynamic endpoint resolution in distributed systems,
/// allowing clients to locate and communicate with services without hardcoding endpoint
/// addresses. The route entity can be stored in configuration, databases, or service
/// registries, enabling flexible deployment topologies and runtime service reconfiguration.
/// This is fundamental for microservices architectures where services need to discover
/// and communicate with each other dynamically.
/// </remarks>
public interface IRoute
	: IEntity<Guid>
{
	/// <summary>
	/// Gets the name of the service this route targets.
	/// </summary>
	/// <value>
	/// A string representing the service identifier.
	/// </value>
	/// <remarks>
	/// The service name uniquely identifies which service this route provides access to,
	/// enabling clients to look up routes by service name when they need to communicate
	/// with a specific service.
	/// </remarks>
	string Service { get; init; }

	/// <summary>
	/// Gets the communication protocol for this route.
	/// </summary>
	/// <value>
	/// A <see cref="RouteProtocol"/> value indicating the protocol to use.
	/// </value>
	/// <remarks>
	/// The protocol determines how clients should connect to the service, affecting
	/// the choice of client library, message serialization, and available features.
	/// </remarks>
	RouteProtocol Protocol { get; init; }

	/// <summary>
	/// Gets the endpoint URL for this route.
	/// </summary>
	/// <value>
	/// A string containing the full URL to the service endpoint.
	/// </value>
	/// <remarks>
	/// The URL specifies the network address where the service can be reached, including
	/// the protocol scheme, host, port, and optionally a base path. Clients use this URL
	/// to establish connections to the service.
	/// </remarks>
	string Url { get; init; }
}
