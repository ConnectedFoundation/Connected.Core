using System.Collections.Immutable;
using System.Data;

namespace Connected.Storage.Oracle;

/// <summary>
/// Provides Oracle database reading capabilities for entity queries.
/// </summary>
/// <typeparam name="T">The entity type to read from the database.</typeparam>
/// <remarks>
/// This class extends <see cref="DatabaseCommand"/> to implement <see cref="IStorageReader{T}"/>
/// for executing SELECT queries against Oracle databases. It handles result materialization with
/// Oracle-specific type conversions (NUMBER to numeric types, VARCHAR2/CLOB to strings, RAW/BLOB
/// to byte arrays), connection lifecycle management, and transaction handling based on connection
/// mode. For Shared connections, the connection remains open for reuse in the pool; for Isolated
/// connections, the connection is committed and closed after each operation. The reader supports
/// both single entity retrieval (Select) and collection queries (Query), as well as raw DataReader
/// access (OpenReader) for advanced scenarios. Connection and transaction cleanup is performed
/// automatically in finally blocks to ensure Oracle resources are released even when exceptions occur.
/// </remarks>
internal sealed class DatabaseReader<T>(IStorageOperation operation, IStorageConnection connection)
	: DatabaseCommand(operation, connection), IStorageReader<T>
{
	/// <summary>
	/// Executes a query and returns all matching entities as an immutable list.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous query operation. The task result contains
	/// an immutable list of entities. Returns an empty list if the connection is null.
	/// </returns>
	/// <remarks>
	/// This method executes the storage operation's SQL query against the Oracle database
	/// and materializes all results into an immutable list. Oracle-specific type conversions
	/// are performed automatically by the connection layer. For Isolated connection mode,
	/// the transaction is committed and the connection is closed after query execution,
	/// returning the connection to the pool. Connection cleanup is performed in a finally
	/// block to ensure resources are released even if an exception occurs during query execution.
	/// </remarks>
	public async Task<IImmutableList<T>> Query()
	{
		if (Connection is null)
			return ImmutableList<T>.Empty;

		try
		{
			/*
			 * Execute query and materialize results from Oracle result set
			 */
			var result = await Connection.Query<T>(this);

			/*
			 * Commit transaction for isolated connections
			 */
			if (Connection.Mode == StorageConnectionMode.Isolated)
				await Connection.Commit();

			return result;
		}
		finally
		{
			/*
			 * Cleanup isolated connections and return to pool
			 */
			if (Connection.Mode == StorageConnectionMode.Isolated)
			{
				await Connection.Close();
				await Connection.DisposeAsync();
			}
		}
	}

	/// <summary>
	/// Executes a query and returns a single entity or null.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous query operation. The task result contains
	/// the entity if found, or <c>null</c> if no matching entity exists or the connection is null.
	/// </returns>
	/// <remarks>
	/// This method executes the storage operation's SQL query against the Oracle database
	/// and returns the first matching entity or null if no match is found. Oracle-specific
	/// type conversions are applied automatically. For Isolated connection mode, the transaction
	/// is committed and the connection is closed after query execution. Connection cleanup is
	/// performed in a finally block to ensure resources are released even if an exception occurs
	/// during query execution. This method is typically used for queries that return a single
	/// result by primary key or unique constraint.
	/// </remarks>
	public async Task<T?> Select()
	{
		try
		{
			if (Connection is null)
				return default;

			/*
			 * Execute query and retrieve single entity with Oracle type conversions
			 */
			var result = await Connection.Select<T>(this);

			/*
			 * Commit transaction for isolated connections
			 */
			if (Connection.Mode == StorageConnectionMode.Isolated)
				await Connection.Commit();

			return result;
		}
		finally
		{
			/*
			 * Cleanup isolated connections and return to pool
			 */
			if (Connection.Mode == StorageConnectionMode.Isolated)
			{
				await Connection.Close();
				await Connection.DisposeAsync();
			}
		}
	}

	/// <summary>
	/// Opens a data reader for streaming query results.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// an <see cref="IDataReader"/> for reading query results, or <c>null</c> if the operation fails.
	/// </returns>
	/// <remarks>
	/// This method provides low-level access to Oracle query results through a DataReader,
	/// allowing for efficient streaming of large result sets without materializing all data
	/// into memory. The caller is responsible for disposing the returned OracleDataReader when
	/// done. This method is useful for scenarios requiring custom result processing, working
	/// with Oracle-specific types (REF CURSOR, XMLTYPE, CLOB, BLOB), or when working with large
	/// datasets that should not be fully materialized. The connection remains open while the
	/// reader is active and must be closed by the caller.
	/// </remarks>
	public async Task<IDataReader?> OpenReader()
	{
		return await Connection.OpenReader(this);
	}
}
