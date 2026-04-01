using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues;
/// <summary>
/// The default implementation of the queue client.
/// </summary>
public abstract class QueueClient<TEntity, TDto>(IStorageProvider storage, IQueueMessageCache cache)
	: Middleware, IQueueClient<TDto>
	where TDto : IDto
	where TEntity : IQueueMessage
{
	/// <summary>
	/// Gets the queue message which is proccesed by a client.
	/// </summary>
	protected IQueueMessage Message { get; private set; } = default!;
	/// <summary>
	/// A Dto object containing the necessary information for retrieving the data to
	/// be processed.
	/// </summary>
	protected TDto Dto { get; private set; } = default!;
	/// <summary>
	/// A cancellation token that should be used for determining if the request has been cancelled.
	/// </summary>
	protected CancellationToken Cancel { get; private set; }

	protected virtual string? Batch { get; set; }
	protected virtual TimeSpan? BatchTimeout { get; set; }
	protected virtual DateTimeOffset? NextVisible { get; set; }
	protected virtual int Priority { get; set; }
	protected virtual DateTimeOffset? Expire { get; set; }
	protected int MaxDequeueCount { get; set; } = 10;
	/// <summary>
	/// Starts the processing of the queue message.
	/// </summary>
	/// <param name="message">The queue message that a client should process.</param>
	/// <param name="cancel">A cancellation token that should be used for determining if the request has been cancelled.</param>
	public async Task Invoke(IQueueMessage message, CancellationToken cancel = default)
	{
		Message = message;
		Dto = (TDto)message.Dto;
		Cancel = cancel;

		await OnInvoke();
	}
	/// <summary>
	/// Process the queue message.
	/// </summary>
	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}

	public async Task Invoke(TDto dto)
	{
		Dto = dto;

		if (!await Validate())
			return;

		var dtoTypeName = Dto.GetType().FullName ?? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} '{Dto.GetType()}'");
		var instance = typeof(TEntity).CreateInstance<TEntity>().Required();

		instance.GetType().GetProperty(nameof(IQueueMessage.Dto))?.SetValue(instance, Dto);
		instance.GetType().GetProperty(nameof(IQueueMessage.DtoTypeName))?.SetValue(instance, $"{dtoTypeName}, {Dto.GetType().Assembly.GetName().Name}");
		instance.GetType().GetProperty(nameof(IQueueMessage.Created))?.SetValue(instance, DateTimeOffset.UtcNow);
		instance.GetType().GetProperty(nameof(IQueueMessage.Client))?.SetValue(instance, GetType());
		instance.GetType().GetProperty(nameof(IQueueMessage.Batch))?.SetValue(instance, Batch);
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
		if (string.IsNullOrWhiteSpace(Batch))
			return true;

		var existing = await cache.Select(GetType(), Batch);

		if (existing is not TEntity entity)
			return true;

		if (NextVisible is not null)
		{
			if (BatchTimeout is not null)
			{
				/*
				 * Timeout has been set for the batch, 
				 * check if the existing message has been created longer than the timeout ago 
				 * and if so allow inserting a new message with the same batch value to unblock the queue.
				 */
				if (entity.Created.Add(BatchTimeout.GetValueOrDefault()) < DateTimeOffset.UtcNow)
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

	protected async Task Ping(TimeSpan nextVisible)
	{
		var existing = (await cache.Select(Message.PopReceipt.GetValueOrDefault())).Required<TEntity>();
		var modified = existing.Clone();

		modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, DateTimeOffset.UtcNow.Add(nextVisible));
		modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

		modified = await storage.Open<TEntity>().Update(modified, async (entity) =>
		{
			var cloned = entity.Clone();

			cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(nextVisible));
			cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

			await Task.CompletedTask;

			return cloned;
		}, async () =>
		{
			await cache.Refresh(existing.Id);

			return (await cache.Select(existing.Id)).Required<TEntity>();
		}, new CallerContext());

		if (modified is not null)
			await cache.Update(modified);
	}
}