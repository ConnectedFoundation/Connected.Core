using Connected.Entities.Protection;
using Connected.Reflection;
using Connected.Services;

namespace Connected.Entities;

internal class EntityProtectionService(IMiddlewareService middlewares) : IEntityProtectionService
{
	public async Task Invoke<TEntity>(IEntityProtectionDto<TEntity> dto)
		where TEntity : IEntity
	{
		var items = await middlewares.Query<IEntityProtector<TEntity>>();

		if (items.Count == 0)
			return;

		foreach (var middleware in items)
			await middleware.Invoke(dto);
	}
}