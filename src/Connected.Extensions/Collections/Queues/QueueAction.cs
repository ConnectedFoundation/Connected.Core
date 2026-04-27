using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// Provides an abstract base class for implementing queue action handlers with built-in message visibility management.
/// </summary>
/// <typeparam name="TDto">The data transfer object type containing the message payload.</typeparam>
/// <remarks>
/// QueueAction simplifies the implementation of queue message processors by providing protected access to
/// message context (the message itself, the DTO payload, and cancellation token) and a ping mechanism for
/// extending message visibility during long-running operations.
/// The base class handles the infrastructure concerns of message processing:
/// - Extracting the DTO from the message envelope
/// - Managing cancellation tokens for graceful shutdown
/// - Providing the ping callback for visibility window extension
/// Derived classes override <see cref="OnInvoke"/> to implement domain-specific processing logic.
/// For operations exceeding the default visibility timeout (typically 30 seconds), derived implementations should
/// call <see cref="Ping"/> periodically to extend the message's NextVisible timestamp, preventing the message from
/// reappearing in the queue and causing duplicate processing.
/// Actions are automatically registered through dependency injection and resolved at runtime based on the
/// message's Action property.
/// </remarks>
public abstract class QueueAction<TDto>
	: IQueueAction<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Gets the queue message currently being processed.
	/// </summary>
	/// <remarks>
	/// This property provides access to message metadata including creation timestamp, dequeue count,
	/// priority, expiration, and pop receipt. It is populated by the <see cref="Invoke"/> method before
	/// <see cref="OnInvoke"/> is called.
	/// </remarks>
	protected IQueueMessage Message { get; private set; } = default!;

	/// <summary>
	/// Gets the data transfer object extracted from the queue message.
	/// </summary>
	/// <remarks>
	/// The DTO contains the business payload for processing, typically including identifiers or parameters
	/// needed to retrieve and process the relevant data. It is deserialized from the message's binary Dto property
	/// and cast to the strongly-typed TDto.
	/// </remarks>
	protected TDto Dto { get; private set; } = default!;

	/// <summary>
	/// Gets the cancellation token for the current processing operation.
	/// </summary>
	/// <remarks>
	/// The cancellation token signals when processing should be aborted, typically during application shutdown.
	/// Derived implementations should pass this token to any async operations and check it periodically during
	/// long-running work to enable graceful cancellation.
	/// </remarks>
	protected CancellationToken Cancel { get; private set; }

	/// <summary>
	/// Gets or sets the callback used to extend message visibility during long-running operations.
	/// </summary>
	/// <remarks>
	/// This callback is set by the <see cref="QueueJob{TEntity, TCache}"/> before invoking the action.
	/// It enables the action to notify the queue system that processing is still in progress, preventing
	/// the message from becoming visible again and being dequeued by another worker.
	/// This is an internal implementation detail not intended for direct use by derived classes.
	/// </remarks>
	internal Func<Task>? PingCallback { get; set; }

	/// <summary>
	/// Implements the IQueueAction interface by extracting message context and invoking the derived processing logic.
	/// </summary>
	/// <param name="message">The queue message to process.</param>
	/// <param name="cancel">The cancellation token for the operation.</param>
	/// <returns>A task representing the asynchronous processing operation.</returns>
	/// <inheritdoc/>
	public async Task Invoke(IQueueMessage message, CancellationToken cancel = default)
	{
		/*
		 * Initialize the protected context properties from the message envelope.
		 * This provides derived classes with strongly-typed access to the message, DTO, and cancellation token.
		 */
		Message = message;
		Dto = (TDto)message.Dto;
		Cancel = cancel;

		/*
		 * Invoke the derived implementation's processing logic.
		 */
		await OnInvoke();
	}

	/// <summary>
	/// When overridden in a derived class, implements the message processing logic.
	/// </summary>
	/// <returns>A task representing the asynchronous processing operation.</returns>
	/// <remarks>
	/// This method is called after the message context has been initialized with the message, DTO, and
	/// cancellation token. Derived classes implement domain-specific processing logic here.
	/// For operations that may exceed the message visibility timeout (typically 30 seconds), implementations
	/// should call <see cref="Ping"/> periodically to extend the visibility window and prevent message reappearance.
	/// Any unhandled exceptions will cause the message to be requeued for retry up to MaxDequeueCount attempts.
	/// </remarks>
	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// Extends the message visibility window to prevent reappearance in the queue during long-running operations.
	/// </summary>
	/// <param name="nextVisible">The time span from now when the message should become visible again if processing has not completed.</param>
	/// <returns>A task representing the asynchronous ping operation.</returns>
	/// <remarks>
	/// This method should be called periodically during long-running processing to inform the queue system
	/// that work is still in progress. It updates the message's NextVisible timestamp in both storage and cache,
	/// preventing the message from being dequeued by another worker.
	/// The typical visibility window is 60 seconds, so pinging every 20-30 seconds is recommended for operations
	/// that may exceed this duration.
	/// If the ping callback is not set (which should not occur in normal operation), this method completes without error.
	/// </remarks>
	protected async Task Ping()
	{
		/*
		 * If the ping callback is available, invoke it to extend the message visibility.
		 * This updates the message's NextVisible timestamp in storage and cache.
		 */
		if (PingCallback is not null)
			await PingCallback();
	}
}
