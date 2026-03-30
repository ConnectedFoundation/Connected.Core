using Connected.Services;

namespace Connected.Collections.Queues;

public interface IDebounceContext<TEntity, TCache, TClient, TPrimaryKey>
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
	where TClient : IQueueClient<IPrimaryKeyDto<TPrimaryKey>>
{
	Task Invoke(TPrimaryKey id);
	Task Invoke(TPrimaryKey id, TimeSpan treshold);
	Task Invoke(TPrimaryKey id, TimeSpan treshold, TimeSpan? timeout);
}
