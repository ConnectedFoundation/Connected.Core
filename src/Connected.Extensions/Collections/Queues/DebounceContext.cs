using Connected.Services;

namespace Connected.Collections.Queues;

internal sealed class DebounceContext<TEntity, TCache, TClient, TPrimaryKey>(IQueueService queue)
	: IDebounceContext<TEntity, TCache, TClient, TPrimaryKey>
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
	where TClient : IQueueClient<IPrimaryKeyDto<TPrimaryKey>>

{
	public async Task Invoke(TPrimaryKey id)
	{
		await Debounce(id, TimeSpan.FromSeconds(5), null);
	}

	public async Task Invoke(TPrimaryKey id, TimeSpan treshold)
	{
		await Debounce(id, treshold, null);
	}

	public async Task Invoke(TPrimaryKey id, TimeSpan treshold, TimeSpan? timeout)
	{
		await Debounce(id, TimeSpan.FromSeconds(5), timeout);
	}

	private async Task Debounce(TPrimaryKey id, TimeSpan delay, TimeSpan? timeout)
	{
		var options = new Dto<IInsertOptionsDto>().Value;

		options.Expire = DateTimeOffset.UtcNow.AddHours(8);
		options.Batch = id!.ToString();
		options.NextVisible = DateTimeOffset.UtcNow.Add(delay);
		options.MaxDequeueCount = 10;
		options.Priority = 0;
		options.BatchTimeout = timeout;

		await queue.Insert<TEntity, TCache, TClient, IPrimaryKeyDto<TPrimaryKey>>(Dto.Factory.CreatePrimaryKey(id), options);
	}
}
