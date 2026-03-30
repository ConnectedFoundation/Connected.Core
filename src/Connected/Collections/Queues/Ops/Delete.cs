using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;
using Microsoft.Extensions.Logging;

namespace Connected.Collections.Queues.Ops;

internal sealed class Delete<TEntity, TCache>(TCache cache, IStorageProvider storage, ILogger<Delete<TEntity, TCache>> logger)
	: ServiceAction<IValueDto<Guid>>
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
{
	protected override async Task OnInvoke()
	{
		var item = await cache.Select(Dto.Value);

		if (item is null)
		{
			logger.LogWarning($"{QueueStrings.ErrQueueMessageNull} ('{Dto.Value}')");

			return;
		}

		var instance = typeof(TEntity).CreateInstance<TEntity>().Required();

		instance.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(instance, State.Delete);
		instance.GetType().GetProperty(nameof(IQueueMessage.Id))?.SetValue(instance, item.Id);

		await storage.Open<TEntity>().Update(instance);

		await cache.Delete(Dto.Value);
	}
}