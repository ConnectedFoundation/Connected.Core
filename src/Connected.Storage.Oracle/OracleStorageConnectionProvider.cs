using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Storage.Oracle;

/// <summary>
/// Provides Oracle database connection instances for storage operations.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="StorageConnectionProvider"/> to create and manage
/// Oracle database connections for entity operations. It creates <see cref="OracleDataConnection"/>
/// instances that handle low-level database communication using Oracle.ManagedDataAccess.Core.
/// The provider integrates with the cancellation framework to support graceful connection
/// cancellation. Connections are returned as an immutable list to ensure thread-safe access.
/// The provider supports both shared and isolated connection modes for different transaction
/// scenarios. Each connection instance manages its own OracleConnection from the connection pool
/// and handles proper resource cleanup through disposal patterns. Oracle connection pooling is
/// managed automatically by Oracle.ManagedDataAccess.Core for optimal performance and resource
/// utilization.
/// </remarks>
internal sealed class OracleStorageConnectionProvider(ICancellationContext cancel)
		: StorageConnectionProvider
{
	/// <summary>
	/// Gets the cancellation context for connection operations.
	/// </summary>
	private ICancellationContext Cancel { get; } = cancel;

	/// <summary>
	/// Creates Oracle database connection instances for the specified entity type.
	/// </summary>
	/// <typeparam name="TEntity">The entity type that will use the connections.</typeparam>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// an immutable list of <see cref="IStorageConnection"/> instances.
	/// </returns>
	/// <remarks>
	/// This method creates a single <see cref="OracleDataConnection"/> instance wrapped in an
	/// immutable list. The connection is configured with the cancellation context for graceful
	/// operation cancellation. The connection will be opened when first used and closed based on
	/// the connection mode (Shared connections remain open for reuse in the pool, Isolated
	/// connections are closed after each operation and returned to the pool). The immutable list
	/// ensures thread-safe access to the connection instances. Oracle connection pooling provides
	/// automatic connection management with configurable pool sizes and timeout settings.
	/// </remarks>
	protected override async Task<IImmutableList<IStorageConnection>> OnInvoke<TEntity>()
	{
		return await Task.FromResult(new List<IStorageConnection> { new OracleDataConnection(Cancel) }.ToImmutableList());
	}
}
