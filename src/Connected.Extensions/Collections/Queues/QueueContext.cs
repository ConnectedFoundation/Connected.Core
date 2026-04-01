using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues;
/// <summary>
/// The default implementation of the queue client.
/// </summary>
public abstract class QueueContext<TEntity, TAction, TDto>(IStorageProvider storage, IQueueMessageCache cache)
	: IQueueContext<TAction, TDto>
	where TDto : IDto
	where TEntity : IQueueMessage, new()
	where TAction : IQueueAction<TDto>
{
	protected virtual string? Group
	{
		get
		{
			/*
			 * Check if the DTO implements IPrimaryKeyDto<> and extract the Id property value.
			 * Since IPrimaryKeyDto is generic, we need to use reflection to access the Id property.
			 */
			var dtoType = Dto.GetType();
			var primaryKeyInterface = dtoType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPrimaryKeyDto<>));

			if (primaryKeyInterface is not null)
			{
				var idProperty = primaryKeyInterface.GetProperty(nameof(IPrimaryKeyDto<object>.Id));

				return idProperty?.GetValue(Dto)?.ToString();
			}

			return null;
		}
	}
	protected virtual TimeSpan? DebounceTimeout { get; } = TimeSpan.FromMinutes(1);
	protected virtual DateTimeOffset? NextVisible { get; } = DateTimeOffset.UtcNow.AddSeconds(5);
	protected virtual int Priority { get; }
	protected virtual DateTimeOffset? Expire { get; } = DateTimeOffset.UtcNow.AddMinutes(10);
	protected virtual int MaxDequeueCount { get; } = 10;
	protected TDto Dto { get; private set; } = default!;
	public async Task Invoke(TDto dto)
	{
		Dto = dto;

		if (!await Validate())
			return;

		var dtoTypeName = dto.GetType().FullName ?? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} '{dto.GetType()}'");
		var instance = typeof(TEntity).CreateInstance<TEntity>().Required();

		instance.GetType().GetProperty(nameof(IQueueMessage.Dto))?.SetValue(instance, dto);
		instance.GetType().GetProperty(nameof(IQueueMessage.DtoTypeName))?.SetValue(instance, $"{dtoTypeName}, {dto.GetType().Assembly.GetName().Name}");
		instance.GetType().GetProperty(nameof(IQueueMessage.Created))?.SetValue(instance, DateTimeOffset.UtcNow);
		instance.GetType().GetProperty(nameof(IQueueMessage.Action))?.SetValue(instance, typeof(TAction));
		instance.GetType().GetProperty(nameof(IQueueMessage.Group))?.SetValue(instance, Group);
		instance.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(instance, NextVisible ?? DateTimeOffset.UtcNow);
		instance.GetType().GetProperty(nameof(IQueueMessage.Priority))?.SetValue(instance, Priority);
		instance.GetType().GetProperty(nameof(IQueueMessage.Expire))?.SetValue(instance, Expire ?? DateTime.UtcNow.AddHours(1));
		instance.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(instance, State.Add);
		instance.GetType().GetProperty(nameof(IQueueMessage.MaxDequeueCount))?.SetValue(instance, MaxDequeueCount);

		var st = storage.Open<TEntity>();

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
		if (string.IsNullOrWhiteSpace(Group))
			return true;

		var existing = await cache.Select(GetType(), Group);

		if (existing is not TEntity entity)
			return true;

		if (NextVisible is not null)
		{
			if (DebounceTimeout is not null)
			{
				/*
				 * Timeout has been set for the batch, 
				 * check if the existing message has been created longer than the timeout ago 
				 * and if so allow inserting a new message with the same batch value to unblock the queue.
				 */
				if (entity.Created.Add(DebounceTimeout.GetValueOrDefault()) < DateTimeOffset.UtcNow)
					return true;
			}
			/*
			 * Update only if next visible values differ for more than 1 second to avoid unnecessary updates and cache refreshes.
			 * Also, passed next visible should be greater than the existing one to avoid duplicate processing.
			 */
			if (existing.NextVisible <= NextVisible.Value && existing.NextVisible.Subtract(NextVisible.Value).Duration().TotalSeconds > 1)
			{
				var modified = entity.Clone();

				modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, NextVisible.GetValueOrDefault());

				await storage.Open<TEntity>().Update(modified, async (entity) =>
				{
					modified = entity.Clone();

					modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, NextVisible.GetValueOrDefault());
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
				}, new CallerContext());

				await cache.Update(modified);
			}

			return false;
		}

		return true;
	}
}