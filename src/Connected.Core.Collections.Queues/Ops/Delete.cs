using Connected.Entities;
using Connected.Services;
using Connected.Storage;
using Microsoft.Extensions.Logging;

namespace Connected.Collections.Queues.Ops;

internal sealed class Delete(IQueueCache cache, IStorageProvider storage, ILogger<Delete> logger)
	: ServiceAction<IValueDto<Guid>>
{
	protected override async Task OnInvoke()
	{
		var item = await cache.Select(Dto.Value);

		if (item is null)
		{
			logger.LogWarning($"{QueueStrings.ErrQueueMessageNull} ('{Dto.Value}')");

			return;
		}

		await storage.Open<QueueMessage>().Update(new QueueMessage
		{
			State = State.Delete,
			Id = item.Id
		});

		await cache.Delete(Dto.Value);
	}
}