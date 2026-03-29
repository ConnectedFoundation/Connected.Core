using Connected.Caching;
using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Services.Validation;

namespace Connected.Storage;

internal sealed class UpdateContext<TEntity, TEntityImplementation, TPrimaryKey>(IStorageProvider storage, IValidationContext validation, IEventService events)
	: IUpdateContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	public async Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IPrimaryKeyDto<TPrimaryKey>
	{
		return await Invoke(operation, selector, cache, TransactionContextOptions.All);
	}

	public async Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache,
		TransactionContextOptions options)
		where TDto : IPrimaryKeyDto<TPrimaryKey>
	{
		var entity = operation.SetState(await selector.Select(operation.Dto.CreatePrimaryKey(operation.Dto.Id)) as TEntityImplementation).Required();
		var st = storage.Open<TEntityImplementation>();
		var result = await st.Update(entity.Merge(operation.Dto, State.Update), operation.Dto, async () =>
		{
			if (options.HasFlag(TransactionContextOptions.InvalidateCache) && cache is not null)
			{
				if (cache is IEntityCache<TEntity, TPrimaryKey> entityCache)
					await entityCache.Refresh(operation.Dto.Id);
				else
					await cache.Remove(operation.Dto.Id);
			}

			return operation.SetState(await selector.Select(operation.Dto.CreatePrimaryKey(operation.Dto.Id)) as TEntityImplementation).Required();
		}, operation.Caller);

		if (options.HasFlag(TransactionContextOptions.InvalidateCache) && cache is not null)
		{
			if (cache is IEntityCache<TEntity, TPrimaryKey> entityCache)
				await entityCache.Refresh(operation.Dto.Id);
			else
				await cache.Remove(operation.Dto.Id);
		}

		if (options.HasFlag(TransactionContextOptions.TriggerEvents))
			await events.Updated(operation, selector, operation.Dto.Id);

		return result.Required();
	}
}
