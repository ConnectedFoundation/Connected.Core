using Connected.Caching;
using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage;

internal sealed class InsertContext<TEntity, TEntityImplementation, TPrimaryKey>(IStorageProvider storage, IEventService events)
	: IInsertContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	public async Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation, ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IDto
	{
		return await Invoke(operation, cache, TransactionContextOptions.All);
	}

	public async Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ICacheContainer<TEntity, TPrimaryKey>? cache,
		TransactionContextOptions options)
		where TDto : IDto
	{
		var entity = (await storage.Open<TEntityImplementation>().Update(operation.Dto.AsEntity<TEntityImplementation>(State.Add))).Required();

		operation.SetState(entity);

		if (options.HasFlag(TransactionContextOptions.InvalidateCache) && cache is not null)
		{
			if (cache is IEntityCache<TEntity, TPrimaryKey> entityCache)
				await entityCache.Refresh(entity.Id);
			else
				await cache.Remove(entity.Id);
		}

		if (options.HasFlag(TransactionContextOptions.TriggerEvents))
			await events.Inserted(operation, operation.Caller.Sender, entity.Id);

		return entity;
	}
}
