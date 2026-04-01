using Connected.Authentication;
using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;
using Connected.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Connected.Collections.Queues;
/// <summary>
/// The base class for implementing a queue host.
/// </summary>
public abstract class QueueHost<TEntity, TCache>
	: ScheduledWorker
	where TCache : IQueueMessageCache
	where TEntity : IQueueMessage
{
	/// <summary>
	/// Create a new instance of the QueueHost class.
	/// </summary>
	public QueueHost()
	{
		Timer = TimeSpan.FromMilliseconds(500);

		Dispatcher = new()
		{
			WorkerSize = 1
		};
	}
	private QueueDispatcher<TEntity, TCache> Dispatcher { get; }
	/// <summary>
	/// The interval which will be set from now when dequeued messages
	/// will become visible again.
	/// </summary>
	protected virtual TimeSpan NextVisibleInterval { get; set; } = TimeSpan.FromSeconds(30);
	protected virtual int QueueSize { get => Dispatcher.WorkerSize; set => Dispatcher.WorkerSize = value; }
	protected override sealed async Task OnInvoke(CancellationToken cancel)
	{
		if (Dispatcher.Available <= 0)
			return;

		using var scope = await Scope.Create().WithSystemIdentity();
		var cache = scope.ServiceProvider.GetRequiredService<TCache>();
		var storage = scope.ServiceProvider.GetRequiredService<IStorageProvider>();

		try
		{
			var items = await Query(cache, scope.ServiceProvider.GetRequiredService<IStorageProvider>());

			items = await OnDequeued(items);
			/*
			 * Make sure changes to the queue service get commited.
			 */
			await scope.Commit();

			if (!items.Any())
				return;
			/*
			 * Messages have been receive but it's not necessary we'll process them
			 */
			foreach (var item in items)
			{
				/*
				 * If the host won't accept the message it will be returned to the queue.
				 */
				if (!await Accept(Dispatcher.Queue, item))
				{
					await Update(cache, storage, item.PopReceipt.GetValueOrDefault());
					continue;
				}
				/*
				 * It's accepted. Enqueue it which will trigger the invoke on the queue client.
				 */
				await Dispatcher.Enqueue(item);
			}
		}
		catch
		{
			await scope.Rollback();

			throw;
		}
	}

	protected virtual async Task<bool> Accept(ConcurrentQueue<TEntity> queue, TEntity message)
	{
		return await Task.FromResult(true);
	}

	protected virtual async Task<IImmutableList<TEntity>> OnDequeued(IImmutableList<TEntity> messages)
	{
		return await Task.FromResult(messages);
	}

	private async Task<IImmutableList<TEntity>> Query(IQueueMessageCache cache, IStorageProvider storage)
	{
		/*
		 * First, retrieve candidates.
		 */
		var targets = await SelectTargets(cache, storage);

		if (targets.Count == 0)
			return [];

		var result = new List<TEntity>();
		/*
		 * We must update each candidate so next requests won't get the same results.
		 */
		foreach (var target in targets)
		{
			var entity = target.Clone();

			if (entity is not TEntity typed)
				continue;

			entity.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(entity, DateTimeOffset.UtcNow.Add(NextVisibleInterval));
			entity.GetType().GetProperty(nameof(IQueueMessage.DequeueTimestamp))?.SetValue(entity, DateTimeOffset.UtcNow);
			entity.GetType().GetProperty(nameof(IQueueMessage.DequeueCount))?.SetValue(entity, target.DequeueCount + 1);
			entity.GetType().GetProperty(nameof(IQueueMessage.PopReceipt))?.SetValue(entity, Guid.NewGuid());
			entity.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(entity, State.Update);
			/*
			 * Queues use isolated storages which means they are not part of the shared transaction context.
			 */
			entity = await storage.Open<TEntity>().Update(typed, async (f) =>
			{
				var cloned = f.Clone();

				cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(NextVisibleInterval));
				cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

				await Task.CompletedTask;
				/*
				 * Do the manual merge
				 */
				return cloned;
			}, async () =>
			{
				/*
				 * Concurrency failed. refresh the cache to read it from a database.
				 */
				await cache.Refresh(entity.Id);

				var cached = (await cache.Select(entity.Id)).Required();
				var cloned = cached.Clone();

				cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(NextVisibleInterval));
				cloned.GetType().GetProperty(nameof(IQueueMessage.DequeueTimestamp))?.SetValue(cloned, DateTimeOffset.UtcNow);
				cloned.GetType().GetProperty(nameof(IQueueMessage.DequeueCount))?.SetValue(cloned, cached.DequeueCount + 1);
				cloned.GetType().GetProperty(nameof(IQueueMessage.PopReceipt))?.SetValue(cloned, Guid.NewGuid());
				cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

				await Task.CompletedTask;

				if (cloned is TEntity ce)
					return ce;

				return default;
			}, new CallerContext());

			if (entity is null)
				continue;
			/*
			 * Only if database call succeeds we'll continue.
			 */
			await cache.Update(entity);

			if (entity is TEntity ce)
				result.Add(ce);
		}

		return [.. result];
	}

	private async Task<List<TEntity>> SelectTargets(IQueueMessageCache cache, IStorageProvider storage)
	{
		/*
		 * First, let's make a call that will return all messages for the requested queue.
		 */
		var items = (await cache.Query()).OrderByDescending(f => f.Priority).ThenBy(f => f.NextVisible).ThenBy(f => f.Id);
		var result = new List<TEntity>();

		if (!items.Any())
			return result;

		var targets = new List<TEntity>();
		int? priority = null;

		foreach (var i in items)
		{
			/*
			 * We don't have a cleanup task so this is a good place to check if the message
			 * is invalid.
			 */
			if (i.Expire <= DateTimeOffset.UtcNow || i.DequeueCount >= i.MaxDequeueCount)
			{
				var instance = typeof(TEntity).CreateInstance();

				if (instance is not TEntity te)
					continue;

				instance.GetType().GetProperty(nameof(IQueueMessage.Id))?.SetValue(instance, i.Id);
				instance.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(instance, State.Delete);

				/*
				 * It's invalid. Delete it.
				 */
				await storage.Open<TEntity>().Update(te);
				await cache.Delete(i.Id);

				continue;
			}
			/*
			 * Not yet past due.
			 */
			if (i.NextVisible > DateTimeOffset.UtcNow)
				continue;
			/*
			 * If priority has been passed the algorithm is a bit more complex.
			 */
			if (Dispatcher.MinPriority is not null)
			{
				/*
				 * It's not the first message hence we already have a priority.
				 */
				if (priority is not null)
				{
					/*
					 * Done. Items are sorted by priority and only one set of priorities
					 * is allowed to be returned to the caller.
					 */
					if (i.Priority < priority)
						break;
				}

				priority = i.Priority;
				/*
				 * Same as above. Items have lower priority than requested.
				 */
				if (i.Priority < Dispatcher.MinPriority)
					break;
			}
			/*
			 * It's a valid candidate.
			 */
			if (i is TEntity ti)
				targets.Add(ti);
		}

		if (targets.Count == 0)
			return result;
		/*
		 * Now sort them by priority and next visible than by the insert.
		 */
		var ordered = targets.OrderByDescending(f => f.Priority).ThenBy(f => f.NextVisible).ThenBy(f => f.Id);
		/*
		 * The number of results is less that is allowed by a caller so we can return the entire set.
		 */
		if (ordered.Count() <= Dispatcher.Available)
			return [.. ordered];
		/*
		 * Return only a subset of results, not more than caller requested.
		 */
		return [.. ordered.Take(Dispatcher.Available)];
	}

	private static async Task Update(TCache cache, IStorageProvider storage, Guid popReceipt)
	{
		var existing = (await cache.Select(popReceipt)).Required<TEntity>();
		var modified = existing.Clone();

		modified.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(modified, DateTimeOffset.UtcNow);
		modified.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(modified, State.Update);

		modified = await storage.Open<TEntity>().Update(modified, async (entity) =>
		{
			var cloned = entity.Clone();

			cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow);
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