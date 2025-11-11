namespace Connected.Net.Events.Dtos;

/// <summary>
/// Represents a data transfer object for unsubscribing from events on the event server.
/// </summary>
/// <remarks>
/// This interface defines the parameters required for a client to unsubscribe from events
/// on the event server. Unsubscriptions can target specific service and event combinations,
/// or can be broader if service and event names are not specified. This flexibility allows
/// clients to remove individual subscriptions or clear all subscriptions at once. The
/// connection identifier associates the unsubscription with a specific client connection,
/// ensuring that subscription management is properly scoped to individual connections.
/// </remarks>
public interface IUnsubscribeEventDto
	: IEventServerDto
{
	/// <summary>
	/// Gets or sets the name of the service to unsubscribe from.
	/// </summary>
	/// <value>
	/// A string representing the service identifier, or null to unsubscribe from all services.
	/// </value>
	/// <remarks>
	/// When specified, only subscriptions to events from this service are removed.
	/// When null, subscriptions across all services may be affected based on the
	/// event name filter or complete unsubscription logic.
	/// </remarks>
	string? Service { get; set; }

	/// <summary>
	/// Gets or sets the name of the event to unsubscribe from.
	/// </summary>
	/// <value>
	/// A string representing the event name, or null to unsubscribe from all events.
	/// </value>
	/// <remarks>
	/// When specified, only subscriptions to this specific event type are removed.
	/// When null, subscriptions to all event types may be affected based on the
	/// service name filter or complete unsubscription logic.
	/// </remarks>
	string? Event { get; set; }
}
