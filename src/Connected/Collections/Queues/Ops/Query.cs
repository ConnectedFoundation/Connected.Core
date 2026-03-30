using Connected.Entities;
using Connected.Reflection;
using Connected.Services;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Collections.Queues.Ops;

internal sealed class Query<TEntity, TCache>(TCache cache, IStorageProvider storage)
	: ServiceFunction<IQueryDto, IImmutableList<TEntity>>
	where TEntity : IQueueMessage
	where TCache : IQueueMessageCache<TEntity>
{
	protected override async Task<IImmutableList<TEntity>> OnInvoke()
	{
		/*
		 * First, retrieve candidates.
		 */
		var targets = await SelectTargets();

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

			entity.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(entity, DateTimeOffset.UtcNow.Add(Dto.NextVisible));
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

				cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTime.UtcNow.Add(Dto.NextVisible));
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

				cloned.GetType().GetProperty(nameof(IQueueMessage.NextVisible))?.SetValue(cloned, DateTimeOffset.UtcNow.Add(Dto.NextVisible));
				cloned.GetType().GetProperty(nameof(IQueueMessage.DequeueTimestamp))?.SetValue(cloned, DateTimeOffset.UtcNow);
				cloned.GetType().GetProperty(nameof(IQueueMessage.DequeueCount))?.SetValue(cloned, cached.DequeueCount + 1);
				cloned.GetType().GetProperty(nameof(IQueueMessage.PopReceipt))?.SetValue(cloned, Guid.NewGuid());
				cloned.GetType().GetProperty(nameof(IQueueMessage.State))?.SetValue(cloned, State.Update);

				await Task.CompletedTask;

				if (cloned is TEntity ce)
					return ce;

				return default;
			}, Caller);

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

	private async Task<List<IQueueMessage>> SelectTargets()
	{
		/*
		 * First, let's make a call that will return all messages for the requested queue.
		 */
		var items = (await cache.Query()).OrderByDescending(f => f.Priority).ThenBy(f => f.NextVisible).ThenBy(f => f.Id);
		var result = new List<IQueueMessage>();

		if (!items.Any())
			return result;

		var targets = new List<IQueueMessage>();
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
			if (i.NextVisible > DateTime.UtcNow)
				continue;
			/*
			 * If priority has been passed the algorithm is a bit more complex.
			 */
			if (Dto.Priority is not null)
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
				if (i.Priority < Dto.Priority)
					break;
			}
			/*
			 * It's a valid candidate.
			 */
			targets.Add(i);
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
		if (ordered.Count() <= Dto.MaxCount)
			return [.. ordered];
		/*
		 * Return only a subset of results, not more than caller requested.
		 */
		return [.. ordered.Take(Dto.MaxCount)];
	}
}