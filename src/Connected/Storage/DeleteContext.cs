using Connected.Caching;
using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage;

internal sealed class DeleteContext<TEntity, TEntityImplementation, TPrimaryKey>(IStorageProvider storage, IEventService events)
	: IDeleteContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	public async Task Invoke<TDto>(IServiceOperation<TDto> operation, ISelectionService<TEntity, TPrimaryKey> selector, ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IPrimaryKeyDto<TPrimaryKey>
	{
		await Invoke(operation, selector, cache, TransactionContextOptions.All);
	}

	public async Task Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache,
		TransactionContextOptions options)
		where TDto : IPrimaryKeyDto<TPrimaryKey>
	{
		var entity = operation.SetState(await selector.Select(operation.Dto)).Required();

		(await storage.Open<TEntityImplementation>().Update(operation.Dto.AsEntity<TEntityImplementation>(State.Delete))).Required();

		if (options.HasFlag(TransactionContextOptions.InvalidateCache) && cache is not null)
			await cache.Remove(entity.Id);

		if (options.HasFlag(TransactionContextOptions.TriggerEvents))
			await events.Deleted(operation, operation.Caller.Sender, entity.Id);
	}
}
