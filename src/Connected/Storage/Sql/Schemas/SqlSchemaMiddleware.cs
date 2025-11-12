using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Configuration;
using Connected.Entities;
using Connected.Reflection;
using Connected.Storage.Schemas;
using Connected.Storage.Sharding;
using Connected.Storage.Sharding.Nodes;
using System.Collections.Immutable;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Specifies the type of database constraint for name generation.
/// </summary>
internal enum ConstraintNameType
{
	/// <summary>
	/// Index or unique constraint.
	/// </summary>
	Index = 1,

	/// <summary>
	/// Primary key constraint.
	/// </summary>
	PrimaryKey = 2,

	/// <summary>
	/// Default value constraint.
	/// </summary>
	Default = 3
}

/// <summary>
/// Middleware for synchronizing SQL Server database schemas with entity definitions.
/// </summary>
/// <remarks>
/// This middleware handles schema synchronization for SQL Server databases including support for
/// sharded database configurations. It validates that entity types implement IEntity before
/// processing and orchestrates schema synchronization across both the primary database and all
/// sharding nodes. The middleware operates with priority 0 to ensure it runs before other schema
/// middleware components. It coordinates schema namespace creation and table synchronization,
/// delegating to specialized transaction classes for the actual DDL generation and execution.
/// For sharded entities, the middleware resolves all sharding nodes and performs synchronization
/// on each node to ensure consistent schema across the distributed database infrastructure.
/// </remarks>
[Priority(0)]
internal sealed class SqlSchemaMiddleware(IMiddlewareService middleware, IStorageProvider storage, IConfigurationService configuration)
	: Middleware, ISchemaMiddleware
{
	/// <summary>
	/// Gets the default connection string from configuration.
	/// </summary>
	private string? ConnectionString { get; } = configuration.Storage.Databases.DefaultConnectionString;

	/// <inheritdoc/>
	public async Task<bool> Invoke(ISchemaMiddlewareDto dto)
	{
		/*
		 * Only process entity types that implement IEntity interface.
		 */
		if (!dto.Type.ImplementsInterface<IEntity>())
			return false;

		if (ConnectionString is null)
			throw new NullReferenceException(SR.ErrNoConnectionString);

		/*
		 * Synchronize the schema on the primary database connection.
		 */
		await Synchronize(dto.Schema, ConnectionString);

		/*
		 * First query all sharding middleware because we must perform synchronization
		 * on all nodes.
		 */
		foreach (var connection in await ResolveShardingMiddleware(dto.Type))
			await Synchronize(dto.Schema, connection.ConnectionString);

		return true;
	}

	/// <summary>
	/// Synchronizes a schema on a specific database connection.
	/// </summary>
	/// <param name="schema">The schema definition to synchronize.</param>
	/// <param name="connectionString">The connection string for the target database.</param>
	/// <returns>A task representing the asynchronous synchronization operation.</returns>
	private async Task Synchronize(ISchema schema, string connectionString)
	{
		var args = new SchemaExecutionContext(storage, schema, connectionString);

		/*
		 * Synchronize schema object first.
		 */
		await new SchemaSynchronize().Execute(args);

		/*
		 * Only tables are supported.
		 */
		if (string.IsNullOrWhiteSpace(schema.Type) || string.Equals(schema.Type, SchemaAttribute.SchemaTypeTable, StringComparison.OrdinalIgnoreCase))
			await new TableSynchronize().Execute(args);
	}

	/// <summary>
	/// Resolves all sharding nodes for an entity type.
	/// </summary>
	/// <param name="entityType">The entity type to resolve sharding nodes for.</param>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains an immutable list
	/// of sharding nodes, or an empty list if the entity is not sharded.
	/// </returns>
	/// <remarks>
	/// This method queries registered sharding node providers to determine all database nodes
	/// where the entity should be synchronized. It uses reflection to invoke the provider's
	/// Invoke method dynamically based on the entity type.
	/// </remarks>
	private async Task<IImmutableList<IShardingNode>> ResolveShardingMiddleware(Type entityType)
	{
		var entity = EntitiesExtensions.GetUnderlyingEntity(entityType);

		if (entity is null)
			return [];

		/*
		 * Construct the generic IShardingNodeProvider<> type for the entity.
		 */
		var type = typeof(IShardingNodeProvider<>).MakeGenericType([entity]);
		var providers = await middleware.Query(type);
		var methodName = nameof(IShardingNodeProvider<IEntity>.Invoke);
		var parameterTypes = new Type[] { typeof(IStorageOperation) };
		var method = Methods.ResolveMethod(type, methodName, null, parameterTypes) ?? throw new NullReferenceException($"{Strings.ErrMethodNotFound} ('{type.Name}.{methodName}')");

		/*
		 * Query each provider to get the list of sharding nodes.
		 */
		foreach (var provider in providers)
		{
			var nodes = await method.InvokeAsync(provider, [null]);

			if (nodes is IImmutableList<IShardingNode> items && items.Count != 0)
				return items;
		}

		return [];
	}
}
