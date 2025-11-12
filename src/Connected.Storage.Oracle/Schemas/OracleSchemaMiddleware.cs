using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Configuration;
using Connected.Entities;
using Connected.Reflection;
using Connected.Storage.Schemas;
using Connected.Storage.Sharding;
using Connected.Storage.Sharding.Nodes;
using System.Collections.Immutable;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Middleware for synchronizing Oracle database schemas with entity definitions.
/// </summary>
/// <remarks>
/// This middleware handles schema synchronization for Oracle databases including support for
/// sharded database configurations. It validates that entity types implement IEntity before
/// processing and orchestrates schema synchronization across both the primary database and all
/// sharding nodes. The middleware operates with priority 0 to ensure it runs before other schema
/// middleware components. It coordinates schema (user) validation and table synchronization,
/// delegating to specialized transaction classes for the actual DDL generation and execution.
/// For sharded entities, the middleware resolves all sharding nodes and performs synchronization
/// on each node to ensure consistent schema across the distributed database infrastructure.
/// Oracle-specific features include automatic DDL commits, system catalog queries (ALL_* views),
/// and user/schema synonymity. Note that unlike PostgreSQL, Oracle does not automatically create
/// schemas (users) - they must be created manually by a DBA with appropriate privileges.
/// </remarks>
[Priority(0)]
internal sealed class OracleSchemaMiddleware(IMiddlewareService middleware, IStorageProvider storage, IConfigurationService configuration)
	: Middleware, ISchemaMiddleware
{
	/// <summary>
	/// Gets the default connection string from configuration.
	/// </summary>
	/// <remarks>
	/// Oracle connection strings support Easy Connect, TNS Names, and full descriptor formats.
	/// </remarks>
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
		 * Query all sharding middleware to perform synchronization on all nodes.
		 * Oracle sharding may use different approaches including Oracle Sharding or
		 * manual node distribution.
		 */
		foreach (var connection in await ResolveShardingMiddleware(dto.Type))
			await Synchronize(dto.Schema, connection.ConnectionString);

		return true;
	}

	/// <summary>
	/// Synchronizes a schema on a specific Oracle database connection.
	/// </summary>
	/// <param name="schema">The schema definition to synchronize.</param>
	/// <param name="connectionString">The connection string for the target Oracle database.</param>
	/// <returns>A task representing the asynchronous synchronization operation.</returns>
	/// <remarks>
	/// Oracle schema (user) validation is performed first. Unlike PostgreSQL which creates schemas
	/// automatically, Oracle requires manual user creation by a DBA. Table synchronization then
	/// proceeds using Oracle DDL statements which are automatically committed.
	/// </remarks>
	private async Task Synchronize(ISchema schema, string connectionString)
	{
		var args = new SchemaExecutionContext(storage, schema, connectionString);

		/*
		 * Validate schema (user) exists.
		 * Oracle does not auto-create users/schemas - they must exist.
		 */
		await new SchemaSynchronize().Execute(args);

		/*
		 * Only tables are supported for schema synchronization.
		 * Oracle supports other object types (views, packages, etc.) but they are not
		 * handled by this middleware.
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
	/// where the entity should be synchronized. Oracle supports various sharding strategies
	/// including system-managed sharding, user-defined sharding, and composite sharding. Each
	/// node may have its own connection string and Oracle instance.
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
