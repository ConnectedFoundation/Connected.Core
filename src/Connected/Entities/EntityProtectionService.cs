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

		if (!typeof(TEntity).IsInterface)
		{
			var implementedEntity = typeof(TEntity).ResolveImplementedEntity();

			if (implementedEntity is null)
				return;

			var type = typeof(IEntityProtector<>).MakeGenericType(implementedEntity);
			var typedItems = await middlewares.Query(type);

			foreach (var typedItem in typedItems)
			{
				var method = type.GetMethod(nameof(IEntityProtector<TEntity>.Invoke));

				if (method is null)
					continue;

				await method.InvokeAsync(typedItem, [dto]);
			}
		}
	}
}