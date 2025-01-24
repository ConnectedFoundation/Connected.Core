using Connected.Configuration;
using Connected.Entities;
using Connected.Reflection;
using Connected.Storage.Schemas;
using Connected.Storage.Sharding;
using Connected.Storage.Sharding.Nodes;
using Connected.Storage.Transactions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

namespace Connected.Storage;

internal sealed class ConnectionProvider(IServiceProvider services, IMiddlewareService middleware, ITransactionContext transactions, IConfigurationService configuration)
	: IConnectionProvider, IAsyncDisposable, IDisposable
{
	private List<IStorageConnection> Connections { get; } = [];
	private string? ConnectionString { get; set; }
	private bool Initialized { get; set; }
	public StorageConnectionMode Mode { get; set; } = StorageConnectionMode.Shared;

	public async Task<ImmutableList<IStorageConnection>> Invoke<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity
	{
		Configure();
		/*
		 * Isolated transactions are enabled once transaction context is in completion stage
		 */
		if (transactions.State != MiddlewareTransactionState.Active)
			Mode = StorageConnectionMode.Isolated;

		return dto is ISchemaSynchronizationContext context && context.ConnectionType is not null ? await ResolveSingle<TEntity>(context) : await ResolveMultiple<TEntity>(dto);
	}

	private void Configure()
	{
		if (Initialized)
			return;

		lock (this)
		{
			if (Initialized)
				return;

			Initialized = true;
			ConnectionString = configuration.Storage.Databases.DefaultConnectionString;

			transactions.StateChanged += OnTransactionStateChanged;
		}
	}

	/// <summary>
	/// This method is called if the supplied arguments already provided connection type on which they will perform operations.
	/// </summary>
	/// <remarks>
	/// This method is usually called when synchronizing entities because the synhronization process already knows what connections
	/// should be used.
	/// </remarks>
	/// <param name="context">The context containing information about the connection type and connection string.</param>
	/// <returns>A list of storage connections which should contain only one storage connection.</returns>
	/// <exception cref="NullReferenceException">If the storage connection couldn't be retrieved from the DI container.</exception>
	private async Task<ImmutableList<IStorageConnection>> ResolveSingle<TEntity>(ISchemaSynchronizationContext context)
	{
		var connection = services.GetRequiredService(context.ConnectionType) as IStorageConnection ?? throw new NullReferenceException($"{SR.ErrConnectionNull} '{context.ConnectionType}'");
		var dto = services.GetRequiredService<IStorageConnectionDto>();

		dto.Mode = Mode;
		dto.ConnectionString = context.ConnectionString;

		return [await EnsureConnection<TEntity>(connection, dto)];
	}
	/// <summary>
	/// Returns a list of connections for the specified storage operation.
	/// </summary>
	/// <remarks>
	/// In sharding models this method could return more than one connection for the read operations but only one for the write. In a non sharding model only one connection
	/// should be returned regardless of the operation type.
	/// </remarks>
	/// <returns>A list of storage connections on which the same storage operation should be performed.</returns>
	/// <exception cref="NullReferenceException">If none storage connections couldn't be resolved either from the sharding provider or a default DI container.</exception>
	private async Task<ImmutableList<IStorageConnection>> ResolveMultiple<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity
	{
		var result = new List<IStorageConnection>();
		/*
		 * We need middlewares which provides storage connections.
		 */
		var middlewares = await middleware.Query<IStorageConnectionProvider>();
		/*
		 * First we'll prepare a list of default connections. Note thay won't get initialized
		 * just retrieved from the DI container to get information about what connection type
		 * will be used when executing the operation. Namely, even in a sharding models the
		 * same connection type will be used for all shards but with different connection strings.
		 * This is why sharding providers provide only connection strings instead of connections.
		 */
		var proposedConnections = new List<IStorageConnection>();
		/*
		 * The default connection dto which uses a default connection string provided
		 * by a system configuration.
		 */
		var connectionDto = services.GetRequiredService<IStorageConnectionDto>();

		connectionDto.Mode = Mode == StorageConnectionMode.Isolated ? Mode : dto.ConnectionMode;
		connectionDto.ConnectionString = ConnectionString;
		/*
		 * Now look at the each middleware and try to retrieve at least one connection. If fact,
		 * only one connection should be used by most providers since the default behavior
		 * should not provides distributed storages.
		 */
		foreach (var middleware in middlewares)
		{
			var connections = await middleware.Invoke<TEntity>(dto);

			if (!connections.IsEmpty)
			{
				/*
				 * Middleware provided connection(s). We are happy with that so we
				 * won't look in the other middlewares. This is why the priority is important,
				 * the more specific entities can have their very own providers and once 
				 * identified, we are not interested in the default implementations anymore.
				 */
				foreach (var connection in connections)
					proposedConnections.Add(connection);

				break;
			}
		}
		/*
		 * At this point we must have at least one proposed connection. Otherwise we don't have
		 * an information about connection type which is essential to performing an execute.
		 */
		if (proposedConnections.Count == 0)
			throw new NullReferenceException($"{SR.ErrNoStorageConnection} '{typeof(TEntity)}'");
		/*
		 * Everything goes well, we have a proposed connection. This should always be the case
		 * since the default provider should handle all entities by default.
		 */
		var referenceType = proposedConnections.ElementAt(0).GetType();
		/*
		 * Check if sharding is supported on the entity. If not, the proposed list will be used.
		 */
		var entityType = EntitiesExtensions.GetUnderlyingEntity(typeof(TEntity));

		if (entityType is not null)
		{
			var middlewareType = typeof(IShardingNodeProvider<>).MakeGenericType(entityType);
			var shardingProviders = await middleware.Query(middlewareType);
			var method = Methods.ResolveMethod(middlewareType, nameof(IShardingNodeProvider<IEntity>.Invoke), null, [typeof(IStorageOperation)]) ?? throw new NullReferenceException($"{Strings.ErrMethodNotFound} ('{nameof(IShardingNodeProvider<IEntity>.Invoke)}')");
			var defaultNodeUsed = false;

			foreach (var provider in shardingProviders)
			{
				/*
				 * Sharding query works a little different since they could be theoretically more than one
				 * provider in the DI handling the same Entity.
				 * Provider should always return at least one node even if the data is stored in the default
				 * storage.
				 */
				if (await Methods.InvokeAsync(method, provider, [dto.Operation]) is not ImmutableList<IShardingNode> nodes || nodes.IsEmpty)
					throw new NullReferenceException($"{SR.ErrNoShardingNodes} ('{provider.GetType().Name}')");
				/*
				 * We have a sharding nodes returned by provider. The rest of the process is simple, we just
				 * request a storage connection from the DI.
				 */
				foreach (var node in nodes)
				{
					/*
					 * There is one exception though, if the returned node has an Id property set to 0
					 * it means the default connection string should be used.
					 * But at the same time only one default node should be passed from all providers.
					 */
					if (node.Id == 0)
					{
						if (defaultNodeUsed)
							throw new InvalidOperationException($"{SR.ErrDefaultNodeMany} ('{provider.GetType().Name}')");

						defaultNodeUsed = true;
					}

					var cs = node.Id == 0 ? ConnectionString : node.ConnectionString;

					if (services.GetRequiredService(referenceType) is not IStorageConnection connection)
						throw new NullReferenceException($"{SR.ErrShardingConnectionNull} '{referenceType}'");

					var shardingConnectionDto = services.GetRequiredService<IStorageConnectionDto>();

					shardingConnectionDto.Mode = Mode == StorageConnectionMode.Isolated ? Mode : dto.ConnectionMode;
					shardingConnectionDto.ConnectionString = cs;

					result.Add(await EnsureConnection<TEntity>(connection, shardingConnectionDto));
				}
			}
			/*
			 * If we'got results from the sharding we'll take them. Otherwise, we'll use proposed connections.
			 */
			if (result.Count != 0)
				return [.. result];
		}

		foreach (var proposed in proposedConnections)
			result.Add(await EnsureConnection<TEntity>(proposed, connectionDto));

		return [.. result];
	}

	private async Task<IStorageConnection> EnsureConnection<TEntity>(IStorageConnection connection, IStorageConnectionDto dto)
	{
		await connection.Initialize(dto);

		var connectionString = connection.ConnectionString ?? dto.ConnectionString;

		if (dto.Mode == StorageConnectionMode.Shared
			&& Connections.FirstOrDefault(f => f.GetType() == connection.GetType()
			&& string.Equals(f.ConnectionString, connectionString, StringComparison.Ordinal)) is IStorageConnection existing)
		{
			return existing;
		}

		if (dto.Mode == StorageConnectionMode.Shared)
			Connections.Add(connection);

		return connection;
	}

	private async void OnTransactionStateChanged(object? sender, EventArgs e)
	{
		if (transactions.State == MiddlewareTransactionState.Committing)
			await Commit();
		else if (transactions.State == MiddlewareTransactionState.Reverting)
			await Rollback();
	}

	private async Task Commit()
	{
		foreach (var connection in Connections)
		{
			try
			{
				await connection.Commit();
			}
			catch { }
		}
	}

	private async Task Rollback()
	{
		foreach (var connection in Connections)
		{
			try
			{
				await connection.Rollback();
			}
			catch { }
		}
	}

	public void Dispose()
	{
		OnDisposing(true);
	}

	public async ValueTask DisposeAsync()
	{
		if (transactions?.State == MiddlewareTransactionState.Active)
			await transactions.Rollback();

		foreach (var connection in Connections)
			await connection.DisposeAsync();

		Connections.Clear();

		OnDisposing(false);
		GC.SuppressFinalize(this);
	}

	private void OnDisposing(bool disposing)
	{
		if (!disposing)
			return;

		if (transactions?.State == MiddlewareTransactionState.Active)
			transactions.Rollback().Wait();

		foreach (var connection in Connections)
			connection.Dispose();

		Connections.Clear();
	}
}
