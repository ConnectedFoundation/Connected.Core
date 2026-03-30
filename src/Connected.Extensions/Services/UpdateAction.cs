using Connected.Caching;
using Connected.Entities;
using Connected.Storage;

namespace Connected.Services;

public abstract class UpdateAction<TEntity, TEntityImplementation, TPrimaryKey, TDto>(IUpdateContext<TEntity, TEntityImplementation, TPrimaryKey> context,
	ISelectionService<TEntity, TPrimaryKey> selector, ICacheContainer<TEntity, TPrimaryKey>? cache)
	: ServiceAction<TDto>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
	where TDto : IPrimaryKeyDto<TPrimaryKey>
{
	protected override async Task OnInvoke()
	{
		await context.Invoke(this, selector, cache);
	}
}
