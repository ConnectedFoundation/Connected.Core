using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues.Ops;

internal sealed class Update<TEntity, TCache>(TCache cache, IStorageProvider storage)
	: ServiceAction<IUpdateDto>
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
{
	protected override async Task OnInvoke()
	{
		var existing = (await cache.Select(Dto.Value)).Required<TEntity>();
		var modified = existing.Clone();

		modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, DateTimeOffset.UtcNow.Add(Dto.NextVisible));
		modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

		modified = await storage.Open<TEntity>().Update(modified, async (entity) =>
		{
			var cloned = entity.Clone();

			cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(Dto.NextVisible));
			cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

			await Task.CompletedTask;

			return cloned;
		}, async () =>
		{
			await cache.Refresh(existing.Id);

			return (await cache.Select(existing.Id)).Required<TEntity>();
		}, Caller);

		if (modified is not null)
			await cache.Update(modified);
	}
}