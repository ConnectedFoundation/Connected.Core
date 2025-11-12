using Connected.Data;
using Connected.Entities;
using Connected.Services;

namespace Connected.Collections.Queues;
/// <summary>
/// Represents a single queue message.
/// </summary>
/// <remarks>
/// A queue message represents a unit of queued or deferred work which can be processed in background.
/// </remarks>
public interface IQueueMessage
	: IPrimaryKeyEntity<long>, IPopReceipt
{
	/// <summary>
	/// Date and time the queue message was created.
	/// </summary>
	DateTimeOffset Created { get; init; }
	/// <summary>
	/// The number of times the clients dequeued the message.
	/// </summary>
	/// <remarks>
	/// There are numerous reasons why a queue message gets dequeued multiple times. It could be that not all conditions
	/// were met at the time of processing or that the queue message was not processed soon enough and its pop receipt expired.
	/// In such cases the message returns to the queue and awaits the next client to dequeue it.
	/// </remarks>
	int DequeueCount { get; init; }
	/// <summary>
	/// The timestamp message was last dequeued.
	/// </summary>
	DateTimeOffset? DequeueTimestamp { get; init; }
	/// <summary>
	/// The DTO object which contains information about the message.
	/// </summary>
	/// <remarks>
	/// Most queue messages have a DTO object, typically providing an id of the entity or record for which processing should be performed.
	/// This object should be as compact as possible because queue implementations will often store the message permanently and thus need to
	/// serialize and later deserialize it.
	/// </remarks>
	IDto Dto { get; init; }
	/// <summary>
	/// The type of the client which will dequeue and process this message.
	/// </summary>
	Type Client { get; init; }
	/// <summary>
	/// The expiration date after which, if not processed, the message will be automatically deleted.
	/// </summary>
	DateTimeOffset Expire { get; init; }
	/// <summary>
	/// The optional batch which uniquely identifies the message. There should be only one message in the queue for the combination
	/// of batch, client and queue. If batch is not specified this criteria is ignored.
	/// </summary>
	string? Batch { get; init; }
	/// <summary>
	/// The priority of the message. Message with higher priority will always be dequeued before messages with lower priority.
	/// </summary>
	int Priority { get; init; }
	/// <summary>
	/// The name of the queue. One host should process only one queue, but there can be messages with different clients.
	/// </summary>
	string Queue { get; init; }
	/// <summary>
	/// The maximum number of dequeue tries before the message will be automatically deleted.
	/// </summary>
	int MaxDequeueCount { get; init; }
}