using Connected.Annotations.Entities;
using Connected.Reflection;
using Connected.Services;
using Microsoft.Extensions.Logging;

namespace Connected.Storage.Schemas.Ops;

internal sealed class Update(IMiddlewareService middleware, ILogger<ISchemaService> logger, ISchemaService schemas)
	: ServiceAction<IUpdateSchemaDto>
{
	protected override async Task OnInvoke()
	{
		if (Dto.Schemas is null || Dto.Schemas.Count == 0)
			return;

		var middlewares = await middleware.Query<ISchemaMiddleware>();

		if (middleware is null)
		{
			logger.LogWarning("No ISchemaMiddleware is registered.");
			return;
		}

		foreach (var entity in Dto.Schemas)
		{
			if (!IsPersistent(entity))
				continue;

			logger.LogTrace("Synchronizing entity '{entity}", entity.Name);

			var synchronized = false;
			var dto = Dto.Create<ISelectSchemaDto>();

			dto.Type = entity;

			var schema = await schemas.Select(dto);

			if (schema is null || schema.Ignore)
				continue;

			foreach (var middleware in middlewares)
			{
				/*
				 * Note that sharding synchronization will be handled by the middleware.
				 */
				var middlewareDto = Dto.Create<ISchemaMiddlewareDto>();

				middlewareDto.Type = entity;
				middlewareDto.Schema = schema;

				if (await middleware.Invoke(middlewareDto))
				{
					synchronized = true;
					break;
				}
			}
			/*
			 * We should notify the environment that entity is no synchronized.
			 * Maybe we should throw the exception here because unsynchronized
			 * entities could cause system instabillity.
			 */
			if (!synchronized)
				logger.LogWarning("No middleware synchronized the entity ({entity}).", entity.Name);
		}
	}

	/// <summary>
	/// Determines if the entity supports persistence. Virtual entities does not support persistence which
	/// means they don't have physical storage.
	/// </summary>
	/// <param name="entityType">The type of the entity to check for persistence.</param>
	/// <returns><c>true</c> if the entity supports persistence, <c>false</c> otherwise.</returns>
	private static bool IsPersistent(Type entityType)
	{
		var table = entityType.FindAttribute<TableAttribute>();

		if (table is null || table.Schema is null)
			return false;

		var persistence = entityType.FindAttribute<PersistenceAttribute>();

		return persistence is null || persistence.Mode.HasFlag(PersistenceMode.Write);
	}
}