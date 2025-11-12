using Connected.Services;

namespace Connected.Net.Grpc.Dtos;

/// <summary>
/// Represents a data transfer object for selecting a gRPC channel.
/// </summary>
/// <remarks>
/// This interface defines the parameters required to select or retrieve a gRPC channel
/// for communicating with a specific service. The service type identifies which gRPC
/// service the channel should connect to, enabling type-safe channel selection and
/// management. This DTO is typically used in conjunction with channel pooling or factory
/// patterns to obtain properly configured gRPC channels for making remote procedure calls.
/// </remarks>
public interface ISelectChannelDto
	: IDto
{
	/// <summary>
	/// Gets or sets the service type for which to select a gRPC channel.
	/// </summary>
	/// <value>
	/// A <see cref="Type"/> representing the gRPC service interface or client type.
	/// </value>
	/// <remarks>
	/// The service type identifies which gRPC service the channel should be configured
	/// for, enabling the channel selection logic to return a properly configured channel
	/// with the correct endpoint, authentication, and other service-specific settings.
	/// </remarks>
	Type Service { get; set; }
}
