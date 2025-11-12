using Connected.Annotations.Entities;
using Connected.Reflection;
using Connected.Services;
using Microsoft.Extensions.Logging;

namespace Connected.Storage.Schemas.Ops;

/// <summary>
/// Updates and synchronizes database schemas for entity types.
/// </summary>
/// <remarks>
/// This operation orchestrates the schema synchronization process by iterating through
/// specified entity types and invoking registered middleware components to perform the
/// actual schema updates. It validates that entities support persistence before attempting
/// synchronization and logs the progress and results of each synchronization attempt.
/// The operation supports sharded databases through middleware-based synchronization,
/// allowing storage providers to handle schema updates according to their specific
/// requirements. If no middleware successfully synchronizes an entity, a warning is logged
/// to indicate potential configuration issues that could affect system stability.
/// </remarks>
internal sealed class Update(IMiddlewareService middleware, ILogger<ISchemaService> logger, ISchemaService schemas)
	: ServiceAction<IUpdateSchemaDto>
{
	/// <inheritdoc/>
	protected override async Task OnInvoke()
	{
		/*
		 * Exit early if no schemas are provided for synchronization.
		 */
		if (Dto.Schemas is null || Dto.Schemas.Count == 0)
			return;

		/*
		 * Retrieve all registered schema middleware instances that will handle the actual
		 * synchronization operations for different storage providers.
		 */
		var middlewares = await middleware.Query<ISchemaMiddleware>();

		if (middleware is null)
		{
			logger.LogWarning("No ISchemaMiddleware is registered.");
			return;
		}

		/*
		 * Process each entity type to determine if it requires schema synchronization
		 * and invoke the appropriate middleware to perform the update.
		 */
		foreach (var entity in Dto.Schemas)
		{
			/*
			 * Skip entities that do not support persistence (virtual entities).
			 */
			if (!IsPersistent(entity))
				continue;

			logger.LogTrace("Synchronizing entity '{entity}'", entity.Name);

			var synchronized = false;
			var dto = Dto.Create<ISelectSchemaDto>();

			dto.Type = entity;

			/*
			 * Retrieve the schema definition for the entity type to be synchronized.
			 */
			var schema = await schemas.Select(dto);

			if (schema is null || schema.Ignore)
				continue;

			/*
			 * Iterate through registered middleware instances to find one that can
			 * handle the synchronization for this entity type and storage provider.
			 */
			foreach (var middleware in middlewares)
			{
				/*
				 * Note that sharding synchronization will be handled by the middleware.
				 */
				var middlewareDto = Dto.Create<ISchemaMiddlewareDto>();

				middlewareDto.Type = entity;
				middlewareDto.Schema = schema;

				/*
				 * Invoke the middleware and check if it successfully handled the synchronization.
				 * Break on the first successful synchronization.
				 */
				if (await middleware.Invoke(middlewareDto))
				{
					synchronized = true;
					break;
				}
			}

			/*
			 * We should notify the environment that entity is not synchronized.
			 * Maybe we should throw the exception here because unsynchronized
			 * entities could cause system instability.
			 */
			if (!synchronized)
				logger.LogWarning("No middleware synchronized the entity ({entity}).", entity.Name);
		}
	}

	/// <summary>
	/// Determines if the entity supports persistence. Virtual entities do not support persistence which
	/// means they don't have physical storage.
	/// </summary>
	/// <param name="entityType">The type of the entity to check for persistence.</param>
	/// <returns><c>true</c> if the entity supports persistence, <c>false</c> otherwise.</returns>
	/// <remarks>
	/// An entity is considered persistent if it has a TableAttribute with a valid schema and either
	/// has no PersistenceAttribute or has a PersistenceAttribute with the Write mode flag set.
	/// Virtual entities that are computed or act as views are excluded from physical schema synchronization.
	/// </remarks>
	private static bool IsPersistent(Type entityType)
	{
		var table = entityType.FindAttribute<TableAttribute>();

		if (table is null || table.Schema is null)
			return false;

		var persistence = entityType.FindAttribute<PersistenceAttribute>();

		return persistence is null || persistence.Mode.HasFlag(PersistenceMode.Write);
	}
}