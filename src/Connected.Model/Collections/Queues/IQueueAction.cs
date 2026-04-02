using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// Defines the contract for queue action handlers that process dequeued messages.
/// </summary>
/// <typeparam name="TDto">The data transfer object type containing the message payload.</typeparam>
/// <remarks>
/// Queue actions represent the processing logic executed when a message is dequeued by a <see cref="QueueHost{TEntity, TCache}"/>.
/// Each action is associated with a specific DTO type and is invoked by the dispatcher when a matching message becomes available.
/// Actions are registered automatically through dependency injection and resolved at runtime based on the message's Action property.
/// The action pattern enables asynchronous, background processing with features including:
/// - Message visibility extension through ping callbacks for long-running operations
/// - Automatic retry logic with configurable maximum dequeue attempts
/// - Priority-based processing queue with expiration handling
/// - Cancellation support for graceful shutdown scenarios
/// Actions should implement idempotent logic since messages may be processed multiple times due to transient failures or timeouts.
/// </remarks>
public interface IQueueAction<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Invokes the action to process a dequeued message.
	/// </summary>
	/// <param name="message">The queue message containing the DTO payload, metadata, and tracking information.</param>
	/// <param name="cancel">A cancellation token to signal processing should be aborted.</param>
	/// <returns>A task representing the asynchronous processing operation.</returns>
	/// <remarks>
	/// This method is called by the queue dispatcher when a message matching this action type is dequeued.
	/// Implementations should extract the DTO from the message, perform the required processing, and complete within
	/// the message's visibility window. For long-running operations, use the Ping callback available in <see cref="QueueAction{TDto}"/>
	/// to extend the visibility timeout and prevent message reappearance in the queue.
	/// Any unhandled exceptions will cause the message to be requeued for retry up to MaxDequeueCount attempts.
	/// </remarks>
	Task Invoke(IQueueMessage message, CancellationToken cancel = default);
}