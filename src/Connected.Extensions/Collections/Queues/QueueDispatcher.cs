using Connected.Collections.Concurrent;
using System.Collections.Concurrent;

namespace Connected.Collections.Queues;

internal sealed class QueueDispatcher : Dispatcher<IQueueMessage, QueueWorker>
{
	public int? MinPriority => Queue.IsEmpty ? null : Queue.Min(f => f.Priority);

	public new ConcurrentQueue<IQueueMessage> Queue => base.Queue;
}