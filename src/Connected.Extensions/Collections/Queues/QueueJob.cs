using Connected.Collections.Concurrent;
using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;
using Connected.Threading;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Connected.Collections.Queues;
/// <summary>
/// Provides the dispatcher job implementation for processing individual queue messages.
/// </summary>
/// <typeparam name="TEntity">The concrete queue message entity type.</typeparam>
/// <typeparam name="TCache">The queue message cache type.</typeparam>
/// <param name="storage">The storage provider for message persistence operations.</param>
/// <param name="cache">The queue message cache for state synchronization.</param>
/// <param name="logger">The logger for diagnostic and error messages.</param>
/// <remarks>
/// QueueJob orchestrates the complete lifecycle of processing a single dequeued message:
/// - Extends the message visibility timeout before processing begins
/// - Starts a periodic timeout guard to prevent message reappearance during long-running operations
/// - Resolves and invokes the action handler with the message payload
/// - Sets up ping callbacks for actions that need to extend visibility windows
/// - Deletes the message from storage and cache upon successful completion
/// - Handles expired messages and missing action types gracefully
/// The job runs within a dispatcher worker thread and supports cancellation for graceful shutdown.
/// Messages that fail processing are not explicitly requeued; they become visible again automatically
/// when their visibility timeout expires, enabling automatic retry up to MaxDequeueCount attempts.
/// </remarks>
internal sealed class QueueJob<TEntity, TCache>(IStorageProvider storage, TCache cache, ILogger<QueueJob<TEntity, TCache>> logger)
	: DispatcherJob<TEntity>
	where TCache : IQueueMessageCache
	where TEntity : IQueueMessage
{
	/// <summary>
	/// Gets or sets the timeout guard that periodically extends message visibility during processing.
	/// </summary>
	/// <remarks>
	/// This guard starts before action invocation and pings the message every 20 seconds to extend
	/// the visibility window to 60 seconds from now, preventing message reappearance in the queue.
	/// </remarks>
	private TaskTimeout? Timeout { get; set; }

	/// <summary>
	/// Implements the dispatcher job processing lifecycle including visibility extension, action invocation, and completion.
	/// </summary>
	/// <returns>A task representing the asynchronous processing operation.</returns>
	/// <remarks>
	/// This method coordinates the complete message processing workflow:
	/// 1. Validates the message has a valid action type
	/// 2. Extends visibility timeout to ensure sufficient processing time
	/// 3. Starts a periodic guard to prevent message reappearance during long operations
	/// 4. Executes the action handler
	/// 5. Deletes the message upon successful completion
	/// Any unhandled exceptions are logged and allowed to propagate, causing the message to remain in the queue
	/// for retry on the next dequeue cycle.
	/// </remarks>
	protected override async Task OnInvoke()
	{
		/*
		 * Check if the message has a valid action type.
		 * Old messages may reference types that no longer exist in the runtime, making them unprocessable.
		 */
		if (Dto.Action is null)
		{
			/*
			 * No action type available. Delete the message to prevent it from blocking the queue.
			 */
			await Complete();

			return;
		}

		/*
		 * Extend the message visibility timeout before processing begins.
		 * This ensures we have sufficient time for action resolution and initial setup.
		 */
		await Update();

		/*
		 * Start the periodic timeout guard that extends visibility every 20 seconds.
		 * This prevents the message from becoming visible again if processing exceeds the default window.
		 */
		Timeout = new TaskTimeout(Update, TimeSpan.FromSeconds(20), Cancel);

		/*
		 * Start the guard task.
		 */
		Timeout.Start();

		try
		{
			/*
			 * Execute the action handler to process the message payload.
			 */
			await Execute();
		}
		catch (Exception ex)
		{
			/*
			 * Log the exception but allow it to propagate.
			 * The message will remain in the queue and become visible again for retry.
			 */
			logger.LogError(ex, "{Message}", ex.Message);

			throw;
		}
		finally
		{
			/*
			 * Always stop and dispose the timeout guard regardless of processing outcome.
			 */
			Timeout.Stop();
			Timeout.Dispose();
		}
	}

	/// <summary>
	/// Executes the action handler with the message payload after validation.
	/// </summary>
	/// <returns>A task representing the asynchronous execution operation.</returns>
	/// <remarks>
	/// This method performs final validation, resolves the action service from DI, invokes the Invoke method,
	/// and completes the message by deleting it from storage and cache.
	/// If the action service cannot be resolved or the message has expired, the message is completed without processing.
	/// </remarks>
	private async Task Execute()
	{
		/*
		 * Check if the message is approaching expiration.
		 * If expiration is within 5 seconds, skip processing to avoid starting work that will be abandoned.
		 */
		if (Dto.Expire.AddSeconds(-5) < DateTimeOffset.UtcNow)
			return;

		/*
		 * Resolve the action service from the dependency injection container.
		 */
		var client = CreateClient();

		if (client is null)
		{
			/*
			 * Action service not registered. Delete the message to prevent repeated failures.
			 */
			await Complete();

			return;
		}

		/*
		 * Resolve the Invoke method from the action service using reflection.
		 * This handles both interface implementations and derived class scenarios.
		 */
		var method = client.GetType().ResolveMethod(nameof(IQueueAction<IDto>.Invoke), null, [typeof(IQueueMessage), typeof(CancellationToken)]);

		if (method is null)
			return;

		/*
		 * Invoke the action handler with the message and cancellation token.
		 */
		await method.InvokeAsync(client, Dto, Cancel);

		/*
		 * Processing completed successfully. Delete the message from storage and cache.
		 * It is critical this operation succeeds; otherwise the message will be processed again.
		 */
		await Complete();
	}

	/// <summary>
	/// Completes message processing by deleting it from storage and cache.
	/// </summary>
	/// <returns>A task representing the asynchronous completion operation.</returns>
	/// <remarks>
	/// This method is called after successful processing or when the message is deemed unprocessable
	/// (e.g., action type not found). It ensures the message is removed from both storage and cache
	/// to prevent reappearance in the queue.
	/// </remarks>
	private async Task Complete()
	{
		await Delete();
	}

	/// <summary>
	/// Resolves and creates the action service instance from the dependency injection container.
	/// </summary>
	/// <returns>The action service instance if resolution succeeds; otherwise null.</returns>
	/// <remarks>
	/// This method resolves the action type from the message's Action property, retrieves it from DI,
	/// validates it implements IQueueAction, and sets up the ping callback if it derives from QueueAction base class.
	/// Resolution failures are logged but do not throw exceptions, allowing graceful message completion.
	/// </remarks>
	private object? CreateClient()
	{
		/*
		 * Resolve the action service from the DI container using the type stored in the message.
		 */
		var service = GetService(Dto.Action);

		if (service is null)
		{
			/*
			 * Service not registered. Log an error and return null to trigger message deletion.
			 */
			logger.LogError("Queue action not registered ({Type})", Dto.Action.GetType().ShortName());

			return null;
		}

		/*
		 * Validate the service implements IQueueAction interface.
		 */
		if (!service.GetType().ImplementsInterface(typeof(IQueueAction<>)))
		{
			logger.LogError("Queue does not implement IQueue interface ({Type})", service.GetType().ShortName());

			return null;
		}

		/*
		 * If the service inherits from QueueAction<> base class, set up the ping callback.
		 * This enables the action to extend message visibility during long-running operations.
		 */
		if (IsQueueAction(service.GetType()))
		{
			var pingCallbackProperty = service.GetType().GetProperty(nameof(QueueAction<IDto>.PingCallback), BindingFlags.Instance | BindingFlags.NonPublic);

			/*
			 * Set the PingCallback property to this job's Ping method.
			 * The action can now call Ping to extend the visibility window.
			 */
			pingCallbackProperty?.SetValue(service, new Func<TimeSpan, Task>(Ping));
		}

		return service;
	}

	/// <summary>
	/// Determines whether a type inherits from the QueueAction base class.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type inherits from QueueAction; otherwise false.</returns>
	/// <remarks>
	/// This method walks up the inheritance hierarchy to find the open generic QueueAction type.
	/// It is used to determine whether to set up the ping callback for visibility extension.
	/// </remarks>
	private static bool IsQueueAction(Type type)
	{
		/*
		 * Walk up the inheritance hierarchy to find the QueueAction<> base class.
		 */
		var currentType = type;

		while (currentType is not null)
		{
			if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(QueueAction<>))
				return true;

			currentType = currentType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Deletes the message from storage and cache after successful processing.
	/// </summary>
	/// <returns>A task representing the asynchronous delete operation.</returns>
	/// <remarks>
	/// This method retrieves the message by pop receipt, creates a delete entity, persists it to storage,
	/// and removes the cache entry. If the message cannot be found by pop receipt, a warning is logged
	/// but no exception is thrown.
	/// </remarks>
	private async Task Delete()
	{
		/*
		 * Retrieve the message from cache using the pop receipt assigned during dequeue.
		 */
		var item = await cache.Select(Dto.PopReceipt.GetValueOrDefault());

		if (item is null)
		{
			/*
			 * Message not found in cache. Log a warning and return without throwing.
			 * This can occur if the message was already deleted by another process.
			 */
			logger.LogWarning("{message} ({popReceipt})", SR.ErrQueueMessageNull, Dto.PopReceipt);

			return;
		}

		/*
		 * Create a new entity instance configured for deletion.
		 * Set the Id to match the message being deleted.
		 */
		var instance = typeof(TEntity).CreateInstance<TEntity>().Required();

		instance.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(instance, State.Delete);
		instance.GetType().GetProperty(nameof(IQueueMessage.Id))?.SetValue(instance, item.Id);

		/*
		 * Persist the deletion to storage.
		 */
		await storage.Open<TEntity>().Update(instance);

		/*
		 * Remove the message from the cache using the pop receipt.
		 */
		await cache.Delete(Dto.PopReceipt.GetValueOrDefault());
	}

	/// <summary>
	/// Extends the message visibility timeout to prevent reappearance during processing.
	/// </summary>
	/// <returns>A task representing the asynchronous update operation.</returns>
	/// <remarks>
	/// This method is called before processing begins and periodically by the timeout guard (every 20 seconds).
	/// It updates the NextVisible timestamp to 60 seconds from now, giving ample time for processing to complete
	/// before the message becomes visible to other queue hosts.
	/// Concurrency conflicts are handled by refreshing the cache and retrying with the latest state.
	/// </remarks>
	private async Task Update()
	{
		/*
		 * Retrieve the current message state from cache using the pop receipt.
		 */
		var existing = (await cache.Select(Dto.PopReceipt.GetValueOrDefault())).Required<TEntity>();
		var modified = existing.Clone();

		/*
		 * Extend the NextVisible timestamp to 60 seconds from now.
		 */
		modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(60)));
		modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

		/*
		 * Persist the update to storage with concurrency handling.
		 */
		modified = await storage.Open<TEntity>().Update(modified, async (entity) =>
		{
			var cloned = entity.Clone();

			cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(60)));
			cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

			await Task.CompletedTask;

			return cloned;
		}, async () =>
		{
			/*
			 * Concurrency conflict occurred. Refresh the cache from storage and return the latest version.
			 */
			await cache.Refresh(existing.Id);

			return (await cache.Select(existing.Id)).Required<TEntity>();
		}, new CallerContext());

		/*
		 * Synchronize the cache with the updated message state.
		 */
		if (modified is not null)
			await cache.Update(modified);
	}

	/// <summary>
	/// Extends the message visibility window in response to a ping request from the action handler.
	/// </summary>
	/// <param name="nextVisible">The time span from now when the message should become visible again.</param>
	/// <returns>A task representing the asynchronous ping operation.</returns>
	/// <remarks>
	/// This method is exposed to action handlers through the PingCallback property on QueueAction base class.
	/// Actions call this method during long-running operations to prevent the message from reappearing in the queue.
	/// The update is persisted to storage and cache with concurrency handling.
	/// </remarks>
	public async Task Ping(TimeSpan nextVisible)
	{
		/*
		 * Retrieve the current message state from cache using the pop receipt.
		 */
		var existing = (await cache.Select(Dto.PopReceipt.GetValueOrDefault())).Required<TEntity>();
		var modified = existing.Clone();

		/*
		 * Extend the NextVisible timestamp by the requested duration from now.
		 */
		modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, DateTimeOffset.UtcNow.Add(nextVisible));
		modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

		/*
		 * Persist the update to storage with concurrency handling.
		 */
		modified = await storage.Open<TEntity>().Update(modified, async (entity) =>
		{
			var cloned = entity.Clone();

			cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(nextVisible));
			cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

			await Task.CompletedTask;

			return cloned;
		}, async () =>
		{
			/*
			 * Concurrency conflict occurred. Refresh the cache from storage and return the latest version.
			 */
			await cache.Refresh(existing.Id);

			return (await cache.Select(existing.Id)).Required<TEntity>();
		}, new CallerContext());

		/*
		 * Synchronize the cache with the updated message state.
		 */
		if (modified is not null)
			await cache.Update(modified);
	}
}