using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// Defines the contract for queue context objects that enqueue messages for asynchronous processing.
/// </summary>
/// <typeparam name="TAction">The queue action type that will process the enqueued messages.</typeparam>
/// <typeparam name="TDto">The data transfer object type containing the message payload.</typeparam>
/// <remarks>
/// Queue contexts serve as the entry point for adding messages to the queue system. They encapsulate the logic for
/// creating queue messages with appropriate metadata including priority, expiration, visibility timeout, and group identifiers.
/// The context is responsible for:
/// - Validating messages before enqueueing to prevent duplicates based on group identifiers
/// - Implementing debouncing logic to throttle message creation for the same group
/// - Setting message metadata such as priority, expiration, and visibility windows
/// - Persisting messages to storage and updating the message cache
/// Contexts are generic over both the action type (TAction) that will process the message and the DTO type (TDto) containing
/// the payload. This type safety ensures messages are routed to compatible action handlers at runtime.
/// Messages with the same group identifier are subject to debouncing, meaning only one message per group can be active in the
/// queue at a time. This prevents duplicate processing of the same entity or operation.
/// </remarks>
public interface IQueueContext<TAction, TDto>
	where TAction : IQueueAction<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Enqueues a message for asynchronous processing by the associated action handler.
	/// </summary>
	/// <param name="dto">The data transfer object containing the message payload and processing instructions.</param>
	/// <returns>A task representing the asynchronous enqueue operation.</returns>
	/// <remarks>
	/// This method creates a new queue message with the provided DTO and enqueues it for processing.
	/// The message is assigned metadata including the action type (TAction), creation timestamp, priority,
	/// expiration, and group identifier derived from the DTO.
	/// Before enqueueing, the context validates whether a message with the same group already exists in the queue.
	/// If a duplicate is found and the debounce timeout has not elapsed, the message may be rejected or the existing
	/// message's visibility window may be updated instead of creating a new entry.
	/// The message is persisted to storage and cached to enable efficient dequeue operations by the queue host.
	/// </remarks>
	Task Invoke(TDto dto);
}
