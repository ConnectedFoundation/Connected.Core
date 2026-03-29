using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Storage;

public interface IUpdateContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IPrimaryKeyDto<TPrimaryKey>;

	Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache, TransactionContextOptions options)
		where TDto : IPrimaryKeyDto<TPrimaryKey>;
}
