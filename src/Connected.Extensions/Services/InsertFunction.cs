using Connected.Caching;
using Connected.Entities;
using Connected.Storage;

namespace Connected.Services;

public abstract class InsertFunction<TEntity, TEntityImplementation, TPrimaryKey, TDto>(IInsertContext<TEntity, TEntityImplementation, TPrimaryKey> context,
	ICacheContainer<TEntity, TPrimaryKey>? cache)
	: ServiceFunction<TDto, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
	where TDto : IDto
{
	protected override async Task<TPrimaryKey> OnInvoke()
	{
		return (await context.Invoke(this, cache)).Id;
	}
}
