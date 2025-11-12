using Connected.Services;

namespace Connected.Notifications;

/// <summary>
/// Defines the origin location where an event was raised.
/// </summary>
/// <remarks>
/// This enumeration distinguishes between events that originated within the current
/// application process and events that were raised by remote systems or services.
/// Understanding event origin enables different handling strategies for local versus
/// distributed events, such as avoiding circular propagation or applying different
/// security contexts.
/// </remarks>
public enum EventOrigin
{
	/// <summary>
	/// Indicates the event was raised within the current application process.
	/// </summary>
	InProcess = 1,

	/// <summary>
	/// Indicates the event was raised by a remote system or service.
	/// </summary>
	Remote = 2
}

/// <summary>
/// Describes metadata about an event that was raised in the system.
/// </summary>
/// <remarks>
/// This interface encapsulates comprehensive information about an event including its
/// sender, the service that raised it, the event name, its origin, and the associated
/// data payload. Event descriptors enable event routing, filtering, and handling logic
/// to make decisions based on complete context about the event's source and content.
/// The descriptor supports both in-process and remote event scenarios, providing
/// flexibility for distributed event-driven architectures.
/// </remarks>
public interface IEventDescriptor
{
	/// <summary>
	/// Gets or initializes the operation state that sent the event.
	/// </summary>
	/// <value>
	/// An <see cref="IOperationState"/> representing the sender's operation context,
	/// or null if the sender context is not available.
	/// </value>
	/// <remarks>
	/// The operation state provides access to the execution context and state
	/// associated with the operation that raised the event, enabling event handlers
	/// to access additional context information if needed.
	/// </remarks>
	IOperationState? Sender { get; init; }

	/// <summary>
	/// Gets or initializes the type of the service that raised the event.
	/// </summary>
	/// <value>
	/// A <see cref="Type"/> representing the service interface or class,
	/// or null if the service type is not available.
	/// </value>
	/// <remarks>
	/// The service type helps identify which service or component raised the event,
	/// enabling type-based routing and filtering of event handlers.
	/// </remarks>
	Type? Service { get; init; }

	/// <summary>
	/// Gets or initializes the name of the event.
	/// </summary>
	/// <value>
	/// A string representing the event name, or null if not specified.
	/// </value>
	/// <remarks>
	/// The event name identifies the specific operation or occurrence that triggered
	/// the event, such as "EntityCreated", "EntityUpdated", or custom event names.
	/// This enables event handlers to filter and respond to specific event types.
	/// </remarks>
	string? Event { get; init; }

	/// <summary>
	/// Gets or initializes the origin location where the event was raised.
	/// </summary>
	/// <value>
	/// An <see cref="EventOrigin"/> value indicating whether the event originated
	/// in-process or from a remote source.
	/// </value>
	/// <remarks>
	/// The origin helps distinguish between local and distributed events, enabling
	/// different handling strategies such as preventing circular event propagation
	/// in distributed systems.
	/// </remarks>
	EventOrigin Origin { get; init; }

	/// <summary>
	/// Gets or initializes the data payload associated with the event.
	/// </summary>
	/// <value>
	/// An object containing the event data, typically a DTO, or null if no data is provided.
	/// </value>
	/// <remarks>
	/// The DTO contains the actual data relevant to the event, such as entity state,
	/// operation parameters, or notification details. Event handlers typically cast
	/// this to the expected DTO type for processing.
	/// </remarks>
	object? Dto { get; init; }
}