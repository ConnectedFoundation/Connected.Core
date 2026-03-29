using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Storage;

[Flags]
public enum TransactionContextOptions
{
	None = 0,
	InvalidateCache = 1,
	TriggerEvents = 2,
	All = InvalidateCache | TriggerEvents
}

public interface IPatchContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	Task<TEntity> Invoke<TDto>(IServiceOperation<IPatchDto<TPrimaryKey>> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IDto;

	Task<TEntity> Invoke<TDto>(IServiceOperation<IPatchDto<TPrimaryKey>> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache, TransactionContextOptions options)
		where TDto : IDto;
}
