namespace Connected.Storage.Oracle;

/// <summary>
/// Provides Oracle database writing capabilities for data modification operations.
/// </summary>
/// <remarks>
/// This class extends <see cref="DatabaseCommand"/> to implement <see cref="IStorageWriter"/>
/// for executing INSERT, UPDATE, and DELETE commands against Oracle databases. It handles
/// command execution with Oracle-specific features like colon-prefixed bind variables and
/// RETURNING INTO clauses, transaction management, and connection lifecycle based on connection
/// mode. For Shared connections, the connection remains open for reuse in the pool; for Isolated
/// connections, the transaction is committed and the connection is closed after each operation.
/// The writer returns the number of rows affected by the command, which is useful for validating
/// that operations completed successfully. For INSERT operations with identity columns, the
/// RETURNING INTO clause retrieves generated values from GENERATED AS IDENTITY columns (12c+)
/// or sequence.NEXTVAL. Connection and transaction cleanup is performed automatically in finally
/// blocks to ensure Oracle resources are released even when exceptions occur.
/// </remarks>
internal sealed class DatabaseWriter(IStorageOperation operation, IStorageConnection connection)
		: DatabaseCommand(operation, connection), IStorageWriter
{
	/// <summary>
	/// Executes the database command and returns the number of affected rows.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous execution operation. The task result contains
	/// the number of rows affected by the command (INSERT, UPDATE, or DELETE).
	/// </returns>
	/// <remarks>
	/// This method executes the storage operation's SQL command against the Oracle database
	/// and returns the count of affected rows. For INSERT operations, Oracle's RETURNING INTO
	/// clause is used to retrieve generated identity values into output parameters. For UPDATE
	/// operations with optimistic concurrency, a return value of 0 indicates a concurrency
	/// conflict where another transaction modified the same row. For Isolated connection mode,
	/// the transaction is committed and the connection is closed after command execution,
	/// returning the connection to the pool. Connection cleanup is performed in a finally block
	/// to ensure resources are released even if an exception occurs during execution.
	/// </remarks>
	public async Task<int> Execute()
	{
		try
		{
			/*
			 * Execute command with Oracle bind variables and get affected row count
			 */
			var recordsAffected = await Connection.Execute(this);

			/*
			 * Commit transaction for isolated connections
			 */
			if (Connection.Mode == StorageConnectionMode.Isolated)
				await Connection.Commit();

			return recordsAffected;
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
}
