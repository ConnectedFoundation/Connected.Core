using Connected.Services;

namespace Connected.Notifications;

/// <summary>
/// Defines a middleware component that listens for and handles specific event types.
/// </summary>
/// <typeparam name="TDto">The type of data transfer object associated with the event.</typeparam>
/// <remarks>
/// Event listeners are specialized middleware components that respond to events raised
/// within the application or from remote sources. Each listener is strongly-typed to
/// handle a specific DTO type, ensuring type-safe event processing. Listeners can
/// perform various actions such as updating state, triggering additional operations,
/// sending notifications, or synchronizing data across systems. Multiple listeners can
/// subscribe to the same event type, enabling fan-out event processing patterns. As
/// middleware components, event listeners participate in the application's lifecycle
/// and can be dynamically discovered and initialized.
/// </remarks>
public interface IEventListener<TDto>
	: IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously invokes the event listener with the provided event data.
	/// </summary>
	/// <param name="sender">The operation state that sent the event.</param>
	/// <param name="dto">The data transfer object containing the event data.</param>
	/// <returns>A task that represents the asynchronous event handling operation.</returns>
	/// <remarks>
	/// This method is called when an event matching the listener's DTO type is raised.
	/// Implementations should process the event data and perform any necessary actions
	/// such as updating state, triggering additional operations, or coordinating with
	/// other services. The sender's operation state provides context about the originating
	/// operation, which can be useful for maintaining transactional consistency or
	/// accessing additional state information. Implementations should handle exceptions
	/// gracefully to avoid disrupting other event listeners.
	/// </remarks>
	Task Invoke(IOperationState sender, TDto dto);
}
