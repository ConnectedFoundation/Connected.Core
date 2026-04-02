using Connected.Collections.Concurrent;
using System.Collections.Concurrent;

namespace Connected.Collections.Queues;
/// <summary>
/// Provides a specialized dispatcher for processing queue messages with priority tracking.
/// </summary>
/// <typeparam name="TEntity">The concrete queue message entity type.</typeparam>
/// <typeparam name="TCache">The queue message cache type.</typeparam>
/// <remarks>
/// QueueDispatcher extends the generic Dispatcher pattern to provide queue-specific functionality including:
/// - Minimum priority tracking for optimizing dequeue candidate selection
/// - Concurrent queue access for thread-safe message enqueueing by the queue host
/// The dispatcher maintains a pool of worker threads (QueueJob instances) that process messages concurrently.
/// The WorkerSize property determines how many messages can be processed simultaneously.
/// </remarks>
internal sealed class QueueDispatcher<TEntity, TCache>
	: Dispatcher<TEntity, QueueJob<TEntity, TCache>>
	where TCache : IQueueMessageCache
	where TEntity : IQueueMessage
{
	/// <summary>
	/// Gets the minimum priority of messages currently in the dispatcher queue.
	/// </summary>
	/// <remarks>
	/// This property is used by the queue host to optimize dequeue operations by avoiding candidate selection
	/// for messages with lower priority than those already queued for processing.
	/// Returns null if the queue is empty, indicating no priority filtering should be applied.
	/// </remarks>
	public int? MinPriority => Queue.IsEmpty ? null : Queue.Min(f => f.Priority);

	/// <summary>
	/// Gets the concurrent queue containing messages waiting to be processed by dispatcher jobs.
	/// </summary>
	/// <remarks>
	/// This property exposes the internal queue to the queue host for priority inspection and message enqueueing.
	/// The queue is thread-safe and supports concurrent access from multiple queue host polling cycles.
	/// </remarks>
	public new ConcurrentQueue<TEntity> Queue => base.Queue;
}