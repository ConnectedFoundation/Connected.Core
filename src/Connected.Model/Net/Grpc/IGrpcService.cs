using Connected.Annotations;
using Connected.Net.Grpc.Dtos;
using Grpc.Net.Client;

namespace Connected.Net.Grpc;

/// <summary>
/// Provides services for managing gRPC channels and connections.
/// </summary>
/// <remarks>
/// This service abstracts the creation and management of gRPC channels used for
/// communicating with remote gRPC services. It provides a centralized mechanism for
/// selecting or creating properly configured channels based on service type, enabling
/// consistent channel configuration, connection pooling, and lifecycle management.
/// The service supports service discovery, load balancing, and connection resilience
/// patterns by abstracting the underlying channel selection logic. This allows
/// applications to obtain gRPC channels without managing connection details directly.
/// </remarks>
[Service]
public interface IGrpcService
{
	/// <summary>
	/// Asynchronously selects a gRPC channel for the specified service.
	/// </summary>
	/// <param name="dto">The channel selection parameters containing the service type.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// a configured <see cref="GrpcChannel"/> for the specified service, or null if
	/// no channel could be created or selected.
	/// </returns>
	/// <remarks>
	/// This method returns a gRPC channel configured for communicating with the specified
	/// service type. The channel may be retrieved from a pool, created fresh, or selected
	/// based on routing logic. The returned channel is ready for use in creating gRPC
	/// client instances and making remote procedure calls. Callers should not dispose
	/// of the channel if it is managed by a pool or factory.
	/// </remarks>
	Task<GrpcChannel?> SelectChannel(ISelectChannelDto dto);
}
