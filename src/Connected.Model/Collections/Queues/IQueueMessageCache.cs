using Connected.Caching;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;

public interface IQueueMessageCache<TEntity>
	: IEntityCache<IQueueMessage, long>
{
	Task<IQueueMessage?> Select(Type client, string batch);
	Task<IImmutableList<IQueueMessage>> Query();
	Task<IQueueMessage?> Select(Guid popReceipt);
	Task Delete(Guid popReceipt);
	Task Delete(long id);
	Task<IQueueMessage?> Select(long id);
	Task Update(IQueueMessage message);
}
