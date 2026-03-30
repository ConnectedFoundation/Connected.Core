using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues.Ops;

internal sealed class Insert<TEntity, TCache, TClient, TDto>(TCache cache, IStorageProvider storage)
	: ServiceAction<TDto>
	where TClient : IQueueClient<TDto>
	where TDto : IDto
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
{
	private IStorageProvider Storage { get; } = storage;
	public IInsertOptionsDto Options { get; set; } = default!;

	protected override async Task OnInvoke()
	{
		if (!await Validate())
			return;

		var dtoTypeName = Dto.GetType().FullName ?? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} '{Dto.GetType()}'");

		var instance = typeof(TEntity).CreateInstance<TEntity>().Required();

		instance.GetType().GetProperty(nameof(IQueueMessage.Dto))?.SetValue(instance, Dto);
		instance.GetType().GetProperty(nameof(IQueueMessage.DtoTypeName))?.SetValue(instance, $"{dtoTypeName}, {Dto.GetType().Assembly.GetName().Name}");
		instance.GetType().GetProperty(nameof(IQueueMessage.Created))?.SetValue(instance, DateTimeOffset.UtcNow);
		instance.GetType().GetProperty(nameof(IQueueMessage.Client))?.SetValue(instance, typeof(TClient));
		instance.GetType().GetProperty(nameof(IQueueMessage.Batch))?.SetValue(instance, Options.Batch);
		instance.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(instance, Options.NextVisible ?? DateTimeOffset.UtcNow);
		instance.GetType().GetProperty(nameof(IQueueMessage.Priority))?.SetValue(instance, Options.Priority);
		instance.GetType().GetProperty(nameof(IQueueMessage.Expire))?.SetValue(instance, Options.Expire);
		instance.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(instance, State.Add);
		instance.GetType().GetProperty(nameof(IQueueMessage.MaxDequeueCount))?.SetValue(instance, Options.MaxDequeueCount);

		var st = Storage.Open<TEntity>();

		instance = (await st.Update(instance)).Required();
		instance = (await st.AsEntity(f => f.Id == instance.Id)).Required();

		await cache.Update(instance);
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

		if (existing is not TEntity entity)
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
			 * Also, passed next visible should be greater than the existing one to avoid duplicate processing.
			 */
			if (existing.NextVisible <= Options.NextVisible.Value && existing.NextVisible.Subtract(Options.NextVisible.Value).Duration().TotalSeconds > 1)
			{
				var modified = entity.Clone();

				modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, Options.NextVisible.GetValueOrDefault());

				await Storage.Open<TEntity>().Update(modified, async (entity) =>
				{
					modified = entity.Clone();

					modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, Options.NextVisible.GetValueOrDefault());
					modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

					await Task.CompletedTask;

					return modified;
				}, async () =>
				{
					await cache.Refresh(existing.Id);

					var result = await cache.Select(existing.Id);

					result.Required();

					if (result is TEntity refreshed)
						return refreshed;

					throw new NullReferenceException();
				}, Caller);

				await cache.Update(modified);
			}

			return false;
		}

		return true;
	}
}