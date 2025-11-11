using Connected.Annotations;
using Connected.Net.Dtos;
using Connected.Net.Events.Dtos;

namespace Connected.Net.Events;

/// <summary>
/// Provides services for managing event subscriptions and distributing events to clients.
/// </summary>
/// <remarks>
/// This service implements the server-side infrastructure for real-time event distribution
/// using publish-subscribe patterns. It manages client subscriptions, routes events to
/// subscribed clients, and handles message acknowledgements. The event server enables
/// scenarios such as real-time notifications, live data updates, and distributed event
/// propagation across connected clients. It supports selective subscriptions, connection-based
/// routing, and reliable message delivery with acknowledgement tracking. This forms the
/// foundation for building reactive and event-driven distributed applications.
/// </remarks>
[Service]
public interface IEventServer
{
	/// <summary>
	/// Asynchronously subscribes a client to specific events.
	/// </summary>
	/// <param name="dto">The subscription parameters specifying the connection, service, and event.</param>
	/// <returns>A task that represents the asynchronous subscription operation.</returns>
	/// <remarks>
	/// This method registers a client's interest in receiving specific events from the
	/// event server. The subscription is associated with the client's connection and
	/// filters events based on the specified service and event name. Once subscribed,
	/// the client will receive matching events until they unsubscribe or disconnect.
	/// </remarks>
	Task Subscribe(ISubscribeEventDto dto);

	/// <summary>
	/// Asynchronously unsubscribes a client from events.
	/// </summary>
	/// <param name="dto">The unsubscription parameters specifying the connection and optional service and event filters.</param>
	/// <returns>A task that represents the asynchronous unsubscription operation.</returns>
	/// <remarks>
	/// This method removes a client's subscription to events. The unsubscription can be
	/// scoped to specific service and event combinations, or can remove all subscriptions
	/// for the connection if service and event are not specified. This allows clients to
	/// manage their subscriptions dynamically based on their current needs.
	/// </remarks>
	Task Unsubscribe(IUnsubscribeEventDto dto);

	/// <summary>
	/// Asynchronously sends an event to subscribed clients.
	/// </summary>
	/// <param name="dto">The event data including context, client, payload, service, and event name.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	/// <remarks>
	/// This method publishes an event to all clients that have subscribed to the specified
	/// service and event combination. The sending context controls routing behavior such as
	/// targeting specific connections, filtering by identity, or excluding the sender.
	/// Events are queued for delivery and can be acknowledged by clients for reliable
	/// message delivery tracking.
	/// </remarks>
	Task Send(ISendEventDto dto);

	/// <summary>
	/// Asynchronously processes a message acknowledgement from a client.
	/// </summary>
	/// <param name="dto">The acknowledgement containing the connection and message identifiers.</param>
	/// <returns>A task that represents the asynchronous acknowledgement processing operation.</returns>
	/// <remarks>
	/// This method handles client acknowledgements for received messages, enabling reliable
	/// message delivery tracking. When a client acknowledges a message, it confirms successful
	/// receipt and processing, allowing the event server to update delivery status and
	/// potentially remove the message from retry queues.
	/// </remarks>
	Task Acknowledge(IBoundMessageAcknowledgeDto dto);
}
