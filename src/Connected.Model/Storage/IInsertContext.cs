using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Storage;

public interface IInsertContext<TEntity, TEntityImplementation, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ICacheContainer<TEntity, TPrimaryKey>? cache)
		where TDto : IDto;

	Task<TEntity> Invoke<TDto>(IServiceOperation<TDto> operation,
		ICacheContainer<TEntity, TPrimaryKey>? cache, TransactionContextOptions options)
		where TDto : IDto;
}
