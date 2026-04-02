using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;

namespace Connected.Collections.Queues;
/// <summary>
/// Provides an abstract base class for implementing queue context objects that enqueue messages with debouncing and validation logic.
/// </summary>
/// <typeparam name="TEntity">The concrete queue message entity type.</typeparam>
/// <typeparam name="TAction">The queue action type that will process the messages.</typeparam>
/// <typeparam name="TDto">The data transfer object type containing the message payload.</typeparam>
/// <param name="storage">The storage provider for persisting queue messages.</param>
/// <param name="cache">The queue message cache for synchronizing message state.</param>
/// <remarks>
/// QueueContext implements the standard pattern for enqueueing messages with built-in support for:
/// - Group-based debouncing to prevent duplicate processing
/// - Priority-based message ordering
/// - Expiration and visibility window management
/// - Validation and concurrency handling
/// Derived classes can customize behavior by overriding virtual properties including Group, Priority, DebounceTimeout,
/// NextVisible, Expire, and MaxDequeueCount. The Group property defaults to extracting the Id from DTOs implementing
/// IPrimaryKeyDto, enabling automatic debouncing based on entity identifiers.
/// Messages are persisted to storage and synchronized with the cache to enable efficient dequeue operations by queue hosts.
/// The context handles concurrency conflicts through optimistic locking with automatic retry and cache refresh.
/// </remarks>
public abstract class QueueContext<TEntity, TAction, TDto>(IStorageProvider storage, IQueueMessageCache cache)
	: IQueueContext<TAction, TDto>
	where TDto : IDto
	where TEntity : IQueueMessage, new()
	where TAction : IQueueAction<TDto>
{
	/// <summary>
	/// Gets the group identifier used for message debouncing and duplicate prevention.
	/// </summary>
	/// <remarks>
	/// The group identifier is used to prevent multiple messages for the same logical entity or operation
	/// from being active in the queue simultaneously. By default, this property extracts the Id value from
	/// DTOs implementing IPrimaryKeyDto using reflection.
	/// Derived classes can override this property to provide custom group logic based on DTO content.
	/// If null or empty, group-based validation is bypassed and messages are always enqueued.
	/// </remarks>
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

	/// <summary>
	/// Gets the debounce timeout period for preventing rapid re-enqueueing of messages with the same group.
	/// </summary>
	/// <remarks>
	/// When a message with the same group already exists in the queue and was created more recently than this timeout,
	/// the new message will not be enqueued. Instead, the existing message's visibility window may be updated.
	/// This prevents queue flooding when multiple events trigger enqueueing for the same entity in rapid succession.
	/// Default value is 1 minute. Setting to null disables debounce timeout checking.
	/// </remarks>
	protected virtual TimeSpan? DebounceTimeout { get; } = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Gets the timestamp when the message should become visible for dequeuing.
	/// </summary>
	/// <remarks>
	/// Messages are not visible to queue hosts until this timestamp is reached, enabling delayed processing.
	/// This is useful for implementing scheduled tasks or rate limiting message processing.
	/// Default value is 5 seconds from now. Setting to null uses the current timestamp (immediate visibility).
	/// </remarks>
	protected virtual DateTimeOffset? NextVisible { get; } = DateTimeOffset.UtcNow.AddSeconds(5);

	/// <summary>
	/// Gets the message priority used for ordering during dequeue operations.
	/// </summary>
	/// <remarks>
	/// Messages with higher priority values are dequeued before messages with lower priority.
	/// Within the same priority level, messages are ordered by NextVisible timestamp and then by insertion order (Id).
	/// Default value is 0. Negative values are permitted for below-normal priority.
	/// </remarks>
	protected virtual int Priority { get; }

	/// <summary>
	/// Gets the expiration timestamp after which the message will be automatically deleted if not processed.
	/// </summary>
	/// <remarks>
	/// Expired messages are removed during the dequeue scan without being processed.
	/// This prevents the queue from accumulating obsolete messages for entities that no longer exist or operations
	/// that are no longer relevant.
	/// Default value is 10 minutes from now. Setting to null uses a default of 1 hour.
	/// </remarks>
	protected virtual DateTimeOffset? Expire { get; } = DateTimeOffset.UtcNow.AddMinutes(10);

	/// <summary>
	/// Gets the maximum number of dequeue attempts before the message is automatically deleted.
	/// </summary>
	/// <remarks>
	/// Each time a message is dequeued and fails to complete (due to errors or timeout), the dequeue count is incremented.
	/// Once this limit is reached, the message is considered poison and is deleted during the dequeue scan.
	/// Default value is 10 attempts.
	/// </remarks>
	protected virtual int MaxDequeueCount { get; } = 10;

	/// <summary>
	/// Gets the current DTO being processed during message creation.
	/// </summary>
	/// <remarks>
	/// This property is initialized by the <see cref="Invoke"/> method and is available during validation and
	/// group identifier resolution. It provides access to the DTO payload when implementing custom validation or
	/// group logic in derived classes.
	/// </remarks>
	protected TDto Dto { get; private set; } = default!;

	/// <summary>
	/// Implements the IQueueContext interface by creating and enqueueing a message for asynchronous processing.
	/// </summary>
	/// <param name="dto">The data transfer object containing the message payload.</param>
	/// <returns>A task representing the asynchronous enqueue operation.</returns>
	/// <inheritdoc/>
	public async Task Invoke(TDto dto)
	{
		/*
		 * Initialize the DTO property to make it available during validation and group resolution.
		 */
		Dto = dto;

		/*
		 * Perform validation including group-based debouncing and duplicate detection.
		 * If validation fails, the message is not enqueued.
		 */
		if (!await Validate())
			return;

		/*
		 * Resolve the fully qualified type name of the DTO for serialization metadata.
		 * This enables proper deserialization when the message is dequeued.
		 */
		var dtoTypeName = dto.GetType().FullName ?? throw new NullReferenceException($"{Strings.ErrCannotResolveTypeName} '{dto.GetType()}'");

		/*
		 * Create a new instance of the concrete queue message entity type using reflection.
		 * TEntity is required to have a parameterless constructor.
		 */
		var instance = typeof(TEntity).CreateInstance<TEntity>().Required();

		/*
		 * Populate message properties using reflection since TEntity is a generic parameter.
		 * This approach enables the base class to work with any queue message entity implementation.
		 */
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

		/*
		 * Open a storage context for the queue message entity.
		 */
		var st = storage.Open<TEntity>();

		/*
		 * Persist the message to storage using Update which handles both insert and update operations.
		 * Retrieve the saved instance with generated Id and other server-side values.
		 */
		instance = (await st.Update(instance)).Required();
		instance = (await st.AsEntity(f => f.Id == instance.Id)).Required();

		/*
		 * Synchronize the cache with the newly created message to enable efficient dequeue operations.
		 */
		await cache.Update(instance);
	}

	/// <summary>
	/// Validates whether a new message should be enqueued based on group debouncing rules.
	/// </summary>
	/// <returns>True if the message should be enqueued; false if rejected due to debouncing.</returns>
	/// <remarks>
	/// This method implements the debouncing logic that prevents duplicate processing:
	/// 1. If no group is specified, validation passes immediately
	/// 2. If a message with the same group exists and debounce timeout has not elapsed, reject or update visibility
	/// 3. If debounce timeout has elapsed, allow enqueueing a new message
	/// The method also handles visibility window updates for existing messages when appropriate.
	/// </remarks>
	private async Task<bool> Validate()
	{
		/*
		 * If no group identifier is specified, bypass group-based validation.
		 * Messages without groups are always enqueued.
		 */
		if (string.IsNullOrWhiteSpace(Group))
			return true;

		/*
		 * Query the cache for an existing message with the same action type and group identifier.
		 * This performs the duplicate detection that enables debouncing.
		 */
		var existing = await cache.Select(GetType(), Group);

		/*
		 * If no existing message is found, allow enqueueing.
		 */
		if (existing is not TEntity entity)
			return true;

		/*
		 * Existing message found. Apply debouncing rules based on visibility and timeout settings.
		 */
		if (NextVisible is not null)
		{
			if (DebounceTimeout is not null)
			{
				/*
				 * Check if the debounce timeout has elapsed since the existing message was created.
				 * If the timeout has passed, the queue may be stuck, so allow inserting a new message
				 * to unblock processing.
				 */
				if (entity.Created.Add(DebounceTimeout.GetValueOrDefault()) < DateTimeOffset.UtcNow)
					return true;
			}

			/*
			 * Debounce timeout has not elapsed. Instead of enqueueing a new message, update the
			 * existing message's visibility window if the new value is significantly different (>1 second).
			 * This brings forward the processing time without creating a duplicate.
			 */
			if (existing.NextVisible <= NextVisible.Value && existing.NextVisible.Subtract(NextVisible.Value).Duration().TotalSeconds > 1)
			{
				var modified = entity.Clone();

				/*
				 * Update the NextVisible timestamp to the new value.
				 */
				modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, NextVisible.GetValueOrDefault());

				/*
				 * Persist the update to storage with concurrency handling.
				 * If a concurrency conflict occurs, refresh from cache and retry.
				 */
				await storage.Open<TEntity>().Update(modified, async (entity) =>
				{
					modified = entity.Clone();

					modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, NextVisible.GetValueOrDefault());
					modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

					await Task.CompletedTask;

					return modified;
				}, async () =>
				{
					/*
					 * Concurrency conflict occurred. Refresh the cache entry from storage
					 * and return the latest version for retry.
					 */
					await cache.Refresh(existing.Id);

					var result = await cache.Select(existing.Id);

					result.Required();

					if (result is TEntity refreshed)
						return refreshed;

					throw new NullReferenceException();
				}, new CallerContext());

				/*
				 * Synchronize the cache with the updated message.
				 */
				await cache.Update(modified);
			}

			/*
			 * Reject enqueueing since an existing message with the same group is active.
			 */
			return false;
		}

		return true;
	}
}