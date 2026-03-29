using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues.Ops;

internal sealed class Insert<TClient, TDto>(IQueueCache cache, IStorageProvider storage) : ServiceAction<TDto>
		where TClient : IQueueClient<TDto>
		where TDto : IDto
{
	private IStorageProvider Storage { get; } = storage;
	public IInsertOptionsDto Options { get; set; } = default!;

	protected override async Task OnInvoke()
	{
		if (!await Validate())
			return;

		var dtoTypeName = Dto.GetType().FullName ?? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} '{Dto.GetType()}'");

		var message = new QueueMessage
		{
			Dto = Dto,
			DtoTypeName = $"{dtoTypeName}, {Dto.GetType().Assembly.GetName().Name}",
			Created = DateTime.UtcNow,
			Client = typeof(TClient),
			Batch = Options.Batch,
			NextVisible = Options.NextVisible ?? DateTimeOffset.UtcNow,
			Priority = Options.Priority,
			Expire = Options.Expire,
			Queue = Options.Queue,
			State = State.Add,
			MaxDequeueCount = Options.MaxDequeueCount
		};

		var st = Storage.Open<QueueMessage>(StorageConnectionMode.Isolated);

		message = await st.Update(message) ?? throw new NullReferenceException(Strings.ErrEntityExpected);
		message = await st.Where(f => f.Id == message.Id).AsEntity() ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		await cache.Update(message);
	}

	private async Task<bool> Validate()
	{
		/*
		 * This is performance optimization.
		 * We only enqueue one message with the same batch argument 
       * at the same time if the batch has been specified.
		 */
		if (string.IsNullOrWhiteSpace(Options.Batch))
			return true;

		var existing = await cache.Select(typeof(TClient), Options.Batch);

		if (existing is not QueueMessage entity)
			return true;

		if (Options.NextVisible is not null)
		{
			if (Options.BatchTimeout is not null)
			{
				/*
				 * Timeout has been set for the batch, 
				 * check if the existing message has been created longer than the timeout ago 
				 * and if so allow inserting a new message with the same batch value to unblock the queue.
				 */
				if (entity.Created.Add(Options.BatchTimeout.GetValueOrDefault()) < DateTimeOffset.UtcNow)
					return true;
			}
			/*
			 * Update only if next visible values differ for more than 1 second to avoid unnecessary updates and cache refreshes.
			 */
			if (Math.Abs(existing.NextVisible.Subtract(Options.NextVisible.Value).Duration().TotalSeconds) > 1)
			{
				var modified = entity with
				{
					NextVisible = Options.NextVisible.GetValueOrDefault()
				};

				await Storage.Open<QueueMessage>(StorageConnectionMode.Isolated).Update(modified, async (entity) =>
				{
					modified = entity with
					{
						NextVisible = Options.NextVisible.GetValueOrDefault(),
						State = State.Update
					};

					await Task.CompletedTask;

					return modified;
				}, async () =>
				{
					await cache.Refresh(existing.Id);

					return await cache.Select(existing.Id) as QueueMessage ?? throw new NullReferenceException(Strings.ErrEntityExpected);
				}, Caller);

				await cache.Update(modified);
			}

			return false;
		}

		return true;
	}
}