using Connected.Collections.Concurrent;
using System.Collections.Concurrent;

namespace Connected.Collections.Queues;

internal sealed class QueueDispatcher<TEntity, TCache>
	: Dispatcher<TEntity, QueueJob<TEntity, TCache>>
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
{
	public int? MinPriority => Queue.IsEmpty ? null : Queue.Min(f => f.Priority);

	public new ConcurrentQueue<TEntity> Queue => base.Queue;
}