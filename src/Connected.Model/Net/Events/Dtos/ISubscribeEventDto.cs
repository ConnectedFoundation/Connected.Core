namespace Connected.Net.Events.Dtos;

/// <summary>
/// Represents a data transfer object for subscribing to events on the event server.
/// </summary>
/// <remarks>
/// This interface defines the parameters required for a client to subscribe to specific
/// events from the event server. Subscriptions can be scoped to a particular service and
/// event name, allowing clients to receive only the events they are interested in. This
/// selective subscription mechanism reduces network traffic and client-side processing by
/// filtering events at the server level. The connection identifier associates the subscription
/// with a specific client connection, enabling the event server to route events appropriately.
/// </remarks>
public interface ISubscribeEventDto
	: IEventServerDto
{
	/// <summary>
	/// Gets or sets the name of the service for which events should be received.
	/// </summary>
	/// <value>
	/// A string representing the service identifier.
	/// </value>
	/// <remarks>
	/// The service name filters subscriptions to events raised by a specific service,
	/// allowing clients to receive only events from services they are interested in.
	/// </remarks>
	string Service { get; set; }

	/// <summary>
	/// Gets or sets the name of the event to subscribe to.
	/// </summary>
	/// <value>
	/// A string representing the event name.
	/// </value>
	/// <remarks>
	/// The event name filters subscriptions to specific event types, such as
	/// "EntityCreated" or "EntityUpdated", allowing clients to receive only
	/// the events they need to handle.
	/// </remarks>
	string Event { get; set; }
}
