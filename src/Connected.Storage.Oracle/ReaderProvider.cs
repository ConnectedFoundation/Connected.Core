using Connected.Annotations;

namespace Connected.Storage.Oracle;

/// <summary>
/// Provides Oracle database reader instances for query operations.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="StorageReaderProvider"/> to create reader instances
/// for executing SELECT queries against Oracle databases. It creates <see cref="DatabaseReader{TEntity}"/>
/// instances configured with the current operation and connection. The reader handles result
/// materialization, connection management, and transaction handling based on connection mode
/// (Shared or Isolated). Priority 0 ensures readers are created before other providers in the
/// middleware pipeline. The reader supports both single entity retrieval (Select) and collection
/// queries (Query) with proper connection lifecycle management. Oracle queries use double-quoted
/// identifiers, colon-prefixed bind variables, and proper type conversions from Oracle NUMBER,
/// VARCHAR2, and other Oracle-specific types.
/// </remarks>
[Priority(0)]
internal sealed class ReaderProvider
	: StorageReaderProvider
{
	/// <summary>
	/// Creates a storage reader for the specified entity type.
	/// </summary>
	/// <typeparam name="TEntity">The entity type to create the reader for.</typeparam>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="IStorageReader{TEntity}"/> for executing queries, or <c>null</c> if creation fails.
	/// </returns>
	/// <remarks>
	/// This method creates a <see cref="DatabaseReader{TEntity}"/> configured with the current
	/// storage operation and connection. The reader is responsible for executing the query,
	/// materializing results from Oracle result sets, and managing connection lifecycle based
	/// on the connection mode. For isolated connections, the reader commits transactions and
	/// closes connections after query execution. Oracle-specific type conversions handle NUMBER
	/// to numeric CLR types, VARCHAR2/CLOB to strings, and RAW/BLOB to byte arrays.
	/// </remarks>
	protected override Task<IStorageReader<TEntity>?> OnInvoke<TEntity>()
	{
		return Task.FromResult<IStorageReader<TEntity>?>(new DatabaseReader<TEntity>(Operation, Connection));
	}
}
