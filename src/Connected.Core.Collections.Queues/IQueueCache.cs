using Connected.Caching;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
internal interface IQueueCache : IEntityCache<QueueMessage, long>
{
	Task<bool> Exists(Type client, string batch);
	Task<ImmutableList<QueueMessage>> Query(string queue);
	Task<QueueMessage?> Select(Guid popReceipt);
	Task Delete(Guid popReceipt);
	Task Delete(long id);
	Task<QueueMessage?> Select(long id);
	Task Update(QueueMessage message);
}
