using Connected.Annotations;
using Connected.Services;

namespace Connected.Notifications;

/// <summary>
/// Provides services for publishing events to registered event listeners.
/// </summary>
/// <remarks>
/// This service acts as the central event dispatcher in the application's event-driven
/// architecture. It accepts event notifications from various sources and routes them to
/// all registered event listeners that are subscribed to the specific event type. The
/// service supports both in-process and distributed event scenarios, enabling loosely
/// coupled communication between components. Event publishing is asynchronous, allowing
/// multiple listeners to process events concurrently without blocking the event source.
/// This facilitates reactive programming patterns, audit logging, real-time notifications,
/// and distributed data synchronization.
/// </remarks>
[Service]
public interface IEventService
{
	/// <summary>
	/// Asynchronously publishes an event to all registered listeners.
	/// </summary>
	/// <typeparam name="TService">The type of the service that is raising the event.</typeparam>
	/// <typeparam name="TDto">The type of data transfer object containing the event data.</typeparam>
	/// <param name="dto">The event data including sender information, service type, event name, and payload.</param>
	/// <returns>A task that represents the asynchronous event publishing operation.</returns>
	/// <remarks>
	/// This method publishes an event to all registered listeners that are subscribed to
	/// the specified DTO type. The event is dispatched asynchronously, allowing multiple
	/// listeners to process the event concurrently. The service type parameter helps
	/// identify the source of the event for filtering and routing purposes. Event listeners
	/// can examine the event descriptor to determine whether to process the event based on
	/// sender, service type, event name, or other criteria. Exceptions thrown by individual
	/// listeners should not affect the delivery of events to other listeners.
	/// </remarks>
	Task Insert<TService, TDto>(IInsertEventDto<TService, TDto> dto)
		where TDto : IDto;
}
