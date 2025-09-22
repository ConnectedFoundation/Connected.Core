using Connected.Entities;
using Connected.Services;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Collections.Queues.Ops;

internal sealed class Query(IQueueCache cache, IStorageProvider storage)
	: ServiceFunction<IQueryDto, IImmutableList<IQueueMessage>>
{
	protected override async Task<IImmutableList<IQueueMessage>> OnInvoke()
	{
		/*
		 * First, retrieve candidates.
		 */
		var targets = await SelectTargets();

		if (targets.Count == 0)
			return [];

		var result = new List<IQueueMessage>();
		/*
		 * We must update each candidate so next requests won't get the same results.
		 */
		foreach (var target in targets)
		{
			var entity = target as QueueMessage ?? throw new NullReferenceException(Strings.ErrEntityExpected);

			var message = entity with
			{
				NextVisible = DateTimeOffset.UtcNow.Add(Dto.NextVisible),
				DequeueTimestamp = DateTimeOffset.UtcNow,
				DequeueCount = target.DequeueCount + 1,
				PopReceipt = Guid.NewGuid(),
				State = State.Update
			};
			/*
			 * Queues use isolated storages which means they are not part of the shared transaction context.
			 */
			await storage.Open<QueueMessage>(StorageConnectionMode.Isolated).Update(message, Dto, async () =>
			{
				/*
				 * Concurrency failed. refresh the cache to read it from a database.
				 */
				await cache.Refresh(message.Id);

				return await cache.Select(message.Id) as QueueMessage ?? throw new NullReferenceException(Strings.ErrEntityExpected);
			}, Caller, (entity) =>
			{
				/*
				 * Do the manual merge
				 */
				return Task.FromResult(entity with
				{
					NextVisible = DateTime.UtcNow.Add(Dto.NextVisible),
					State = State.Update
				});
			});
			/*
			 * Only if database call succeeds we'll continue.
			 */
			await cache.Update(message);

			result.Add(message);
		}

		return [.. result];
	}

	private async Task<List<IQueueMessage>> SelectTargets()
	{
		/*
		 * First, let's make a call that will return all messages for the requested queue.
		 */
		var items = (await cache.Query(Dto.Queue)).OrderByDescending(f => f.Priority).ThenBy(f => f.NextVisible).ThenBy(f => f.Id);
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
				/*
				 * It's invalid. Delete it.
				 */
				await storage.Open<QueueMessage>().Update(new QueueMessage { Id = i.Id, State = State.Delete });
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