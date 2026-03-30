using Connected.Caching;
using Connected.Entities;
using Connected.Storage;

namespace Connected.Services;

public abstract class PatchAction<TEntity, TEntityImplementation, TPrimaryKey, TDto>(IPatchContext<TEntity, TEntityImplementation, TPrimaryKey> context,
	ISelectionService<TEntity, TPrimaryKey> selector, ICacheContainer<TEntity, TPrimaryKey>? cache)
	: ServiceAction<IPatchDto<TPrimaryKey>>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
	where TDto : IDto
{
	protected override async Task OnInvoke()
	{
		await context.Invoke<TDto>(this, selector, cache);
	}
}
