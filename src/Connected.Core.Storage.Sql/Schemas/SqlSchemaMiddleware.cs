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

internal enum ConstraintNameType
{
	Index = 1,
	PrimaryKey = 2,
	Default = 3
}

[Priority(0)]
internal sealed class SqlSchemaMiddleware(IMiddlewareService middleware, IStorageProvider storage, IConfigurationService configuration)
	: Middleware, ISchemaMiddleware
{
	private string? ConnectionString { get; } = configuration.Storage.Databases.DefaultConnectionString;

	public async Task<bool> Invoke(ISchemaMiddlewareDto dto)
	{
		if (!dto.Type.ImplementsInterface<IEntity>())
			return false;

		if (ConnectionString is null)
			throw new NullReferenceException(SR.ErrNoConnectionString);

		await Synchronize(dto.Schema, ConnectionString);
		/*
		 * First query all sharding middleware because we must perform synchronization
		 * on all nodes.
		 */
		foreach (var connection in await ResolveShardingMiddleware(dto.Type))
			await Synchronize(dto.Schema, connection.ConnectionString);

		return true;
	}

	private async Task Synchronize(ISchema schema, string connectionString)
	{
		var args = new SchemaExecutionContext(storage, schema, connectionString);
		/*
		 * Synchronize schema object first.
		 */
		await new SchemaSynchronize().Execute(args);
		/*
		 * Only tables are supported
		 */
		if (string.IsNullOrWhiteSpace(schema.Type) || string.Equals(schema.Type, SchemaAttribute.SchemaTypeTable, StringComparison.OrdinalIgnoreCase))
			await new TableSynchronize().Execute(args);
	}

	private async Task<IImmutableList<IShardingNode>> ResolveShardingMiddleware(Type entityType)
	{
		var entity = EntitiesExtensions.GetUnderlyingEntity(entityType);

		if (entity is null)
			return [];

		var type = typeof(IShardingNodeProvider<>).MakeGenericType([entity]);
		var providers = await middleware.Query(type);
		var methodName = nameof(IShardingNodeProvider<IEntity>.Invoke);
		var parameterTypes = new Type[] { typeof(IStorageOperation) };
		var method = Methods.ResolveMethod(type, methodName, null, parameterTypes) ?? throw new NullReferenceException($"{Strings.ErrMethodNotFound} ('{type.Name}.{methodName}')");

		foreach (var provider in providers)
		{
			var nodes = await method.InvokeAsync(provider, [null]);

			if (nodes is IImmutableList<IShardingNode> items && items.Count != 0)
				return items;
		}

		return [];
	}
}
