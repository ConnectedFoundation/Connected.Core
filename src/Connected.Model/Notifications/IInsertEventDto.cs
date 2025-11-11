using Connected.Services;

namespace Connected.Notifications;

/// <summary>
/// Represents a data transfer object for publishing events through the event service.
/// </summary>
/// <typeparam name="TService">The type of the service that is raising the event.</typeparam>
/// <typeparam name="TDto">The type of data transfer object containing the event data.</typeparam>
/// <remarks>
/// This DTO encapsulates all information required to publish an event including the sender's
/// operation state, the service type raising the event, the event name, and the event payload.
/// The strongly-typed service and DTO parameters ensure type-safe event publishing and enable
/// compile-time verification of event routing. This DTO is used as the parameter to the event
/// service's Insert method, providing a structured and consistent way to publish events across
/// the application. The generic type parameters facilitate event listener discovery and routing
/// based on both service type and DTO type.
/// </remarks>
public interface IInsertEventDto<TService, TDto>
	: IDto
	where TDto : IDto
{
	/// <summary>
	/// Gets or sets the operation state that is sending the event.
	/// </summary>
	/// <value>
	/// An <see cref="IOperationState"/> representing the sender's operation context.
	/// </value>
	/// <remarks>
	/// The sender's operation state provides context about the originating operation,
	/// which event listeners can use to access additional state information or maintain
	/// transactional consistency across event processing.
	/// </remarks>
	IOperationState Sender { get; set; }

	/// <summary>
	/// Gets or sets the service instance that is raising the event.
	/// </summary>
	/// <value>
	/// An instance of <typeparamref name="TService"/> representing the service raising the event.
	/// </value>
	/// <remarks>
	/// The service instance enables event listeners to identify the specific service that
	/// raised the event and potentially access service-specific functionality or context.
	/// </remarks>
	TService Service { get; set; }

	/// <summary>
	/// Gets or sets the name of the event being raised.
	/// </summary>
	/// <value>
	/// A string representing the event name, such as "EntityCreated" or "EntityUpdated".
	/// </value>
	/// <remarks>
	/// The event name identifies the specific operation or occurrence that triggered the event,
	/// enabling event listeners to filter and respond to specific event types. Common event
	/// names often correspond to CRUD operations or domain-specific business events.
	/// </remarks>
	string Event { get; set; }

	/// <summary>
	/// Gets or sets the data payload associated with the event.
	/// </summary>
	/// <value>
	/// An instance of <typeparamref name="TDto"/> containing the event data.
	/// </value>
	/// <remarks>
	/// The DTO contains the actual data relevant to the event, such as entity state,
	/// operation parameters, or notification details. Event listeners process this data
	/// to perform their specific handling logic, such as updating caches, sending
	/// notifications, or synchronizing data across systems.
	/// </remarks>
	TDto Dto { get; set; }
}