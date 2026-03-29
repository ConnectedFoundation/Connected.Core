using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Storage;

public interface IDeleteContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	Task Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IPrimaryKeyDto<TPrimaryKey>;

	Task Invoke<TDto>(IServiceOperation<TDto> operation,
		ISelectionService<TEntity, TPrimaryKey> selector,
		ICacheContainer<TEntity, TPrimaryKey>? cache, TransactionContextOptions options)
		where TDto : IPrimaryKeyDto<TPrimaryKey>;
}
