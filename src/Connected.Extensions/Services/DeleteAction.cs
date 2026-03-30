using Connected.Caching;
using Connected.Entities;
using Connected.Storage;

namespace Connected.Services;

public abstract class DeleteAction<TEntity, TEntityImplementation, TPrimaryKey>(IDeleteContext<TEntity, TEntityImplementation, TPrimaryKey> context,
	ISelectionService<TEntity, TPrimaryKey> selector, ICacheContainer<TEntity, TPrimaryKey>? cache)
	: ServiceAction<IPrimaryKeyDto<TPrimaryKey>>
	where TEntity : IEntity<TPrimaryKey>
	where TEntityImplementation : class, TEntity
	where TPrimaryKey : notnull
{
	protected override async Task OnInvoke()
	{
		await context.Invoke(this, selector, cache);
	}
}
