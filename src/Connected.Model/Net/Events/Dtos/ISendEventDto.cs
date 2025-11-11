using Connected.Net.Messaging;
using Connected.Net.Messaging.Dtos;
using Connected.Services;

namespace Connected.Net.Events.Dtos;

/// <summary>
/// Represents a data transfer object for sending events to clients.
/// </summary>
/// <remarks>
/// This interface encapsulates all information required to publish an event through the
/// event server to connected clients. It includes the sending context (filtering and routing
/// information), target client, event payload, service identifier, and event name. The DTO
/// enables flexible event distribution strategies including broadcasting to all clients,
/// targeting specific connections, filtering by identity, and excluding the sender. This
/// supports various real-time communication patterns such as notifications, live updates,
/// and distributed event propagation.
/// </remarks>
public interface ISendEventDto
	: IDto
{
	/// <summary>
	/// Gets or sets the sending context containing routing and filtering information.
	/// </summary>
	/// <value>
	/// An <see cref="ISendContextDto"/> instance specifying how the event should be routed.
	/// </value>
	/// <remarks>
	/// The context determines which clients receive the event based on filters such as
	/// connection identifiers, identity matching, and sender exclusion rules.
	/// </remarks>
	ISendContextDto Context { get; set; }

	/// <summary>
	/// Gets or sets the client associated with this event operation.
	/// </summary>
	/// <value>
	/// An <see cref="IClient"/> instance representing the client context.
	/// </value>
	/// <remarks>
	/// The client provides information about the connection initiating or receiving
	/// the event, including connection and identity information.
	/// </remarks>
	IClient Client { get; set; }

	/// <summary>
	/// Gets or sets the event data payload.
	/// </summary>
	/// <value>
	/// An <see cref="IDto"/> containing the event data to be transmitted.
	/// </value>
	/// <remarks>
	/// The DTO contains the actual event data that will be sent to subscribed clients,
	/// such as entity changes, notifications, or custom event information.
	/// </remarks>
	IDto Dto { get; set; }

	/// <summary>
	/// Gets or sets the name of the service raising the event.
	/// </summary>
	/// <value>
	/// A string representing the service identifier.
	/// </value>
	/// <remarks>
	/// The service name identifies which service is publishing the event, enabling
	/// clients to filter and route events based on their source service.
	/// </remarks>
	string Service { get; set; }

	/// <summary>
	/// Gets or sets the name of the event being sent.
	/// </summary>
	/// <value>
	/// A string representing the event name.
	/// </value>
	/// <remarks>
	/// The event name identifies the specific type of event being published, such as
	/// "EntityCreated" or "EntityUpdated", allowing clients to handle different event
	/// types appropriately.
	/// </remarks>
	string Event { get; set; }
}
