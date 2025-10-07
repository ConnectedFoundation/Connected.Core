using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues.Ops;

internal sealed class Update(IQueueCache cache, IStorageProvider storage)
	: ServiceAction<IUpdateDto>
{
	protected override async Task OnInvoke()
	{
		var existing = await cache.Select(Dto.Value) as QueueMessage ?? throw new NullReferenceException($"{QueueStrings.ErrQueueMessageNull} ('{Dto.Value}')");
		var modified = existing with
		{
			NextVisible = DateTime.UtcNow.Add(Dto.NextVisible),
			State = State.Update
		};

		await storage.Open<QueueMessage>(StorageConnectionMode.Isolated).Update(modified, Dto, async () =>
		{
			await cache.Refresh(existing.Id);

			return await cache.Select(existing.Id) as QueueMessage ?? throw new NullReferenceException(Strings.ErrEntityExpected);
		}, Caller, (entity) =>
		{
			return Task.FromResult(entity with
			{
				NextVisible = DateTime.UtcNow.Add(Dto.NextVisible),
				State = State.Update
			});
		});

		await cache.Update(modified);
	}
}