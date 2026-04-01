using Connected.Caching;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;

public abstract class QueueMessageCache<TEntity>(ICachingService cache, IStorageProvider storage, string key)
	: EntityCache<IQueueMessage, TEntity, long>(cache, storage, key), IQueueMessageCache
	where TEntity : class, IQueueMessage
{
	public async Task<IQueueMessage?> Select(Type client, string batch)
	{
		return await Get(f => string.Equals(f.Batch, batch, StringComparison.OrdinalIgnoreCase) && f.Client == client);
	}

	public async Task<IImmutableList<IQueueMessage>> Query()
	{
		return await All();
	}

	public async Task<IQueueMessage?> Select(Guid popReceipt)
	{
		return await Get(f => f.PopReceipt == popReceipt);
	}

	public async Task<IQueueMessage?> Select(long id)
	{
		return await Get(id);
	}

	public async Task Delete(Guid popReceipt)
	{
		await Remove(f => f.PopReceipt == popReceipt);
	}

	public async Task Delete(long id)
	{
		await Remove(id);
	}

	public Task Update(IQueueMessage message)
	{
		Set(message.Id, message, TimeSpan.Zero);

		return Task.CompletedTask;
	}
}