using Connected.Caching;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;

internal sealed class QueueCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<QueueMessage, long>(cache, storage, MetaData.QueueMessageKey), IQueueCache
{
	public async Task<bool> Exists(Type client, string batch)
	{
		var existing = await Get(f => string.Equals(f.Batch, batch, StringComparison.OrdinalIgnoreCase)
			&& f.Client == client);

		if (existing is null)
			return false;

		if (existing.PopReceipt is null)
			return true;

		return !(existing.NextVisible > DateTimeOffset.UtcNow);
	}

	public async Task<ImmutableList<QueueMessage>> Query(string queue)
	{
		return await Where(f => string.Equals(f.Queue, queue, StringComparison.OrdinalIgnoreCase)) ?? ImmutableList<QueueMessage>.Empty;
	}

	public async Task<QueueMessage?> Select(Guid popReceipt)
	{
		return await Get(f => f.PopReceipt == popReceipt);
	}

	public async Task<QueueMessage?> Select(long id)
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

	public Task Update(QueueMessage message)
	{
		Set(message.Id, message, TimeSpan.Zero);

		return Task.CompletedTask;
	}
}