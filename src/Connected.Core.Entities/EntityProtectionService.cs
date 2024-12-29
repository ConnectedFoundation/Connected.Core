using Connected.Entities.Protection;

namespace Connected.Entities;

internal class EntityProtectionService(IMiddlewareService middlewares) : IEntityProtectionService
{
	public async Task Invoke<TEntity>(IEntityProtectionDto<TEntity> dto)
		where TEntity : IEntity
	{
		var items = await middlewares.Query<IEntityProtector<TEntity>>();

		if (items.IsEmpty)
			return;

		foreach (var middleware in items)
			await middleware.Invoke(dto);
	}
}