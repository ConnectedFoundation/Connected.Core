using Connected.Caching;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
internal interface IQueueCache : IEntityCache<IQueueMessage, long>
{
	Task<bool> Exists(Type client, string batch);
	Task<IImmutableList<IQueueMessage>> Query(string queue);
	Task<IQueueMessage?> Select(Guid popReceipt);
	Task Delete(Guid popReceipt);
	Task Delete(long id);
	Task<IQueueMessage?> Select(long id);
	Task Update(IQueueMessage message);
}
