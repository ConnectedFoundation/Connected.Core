namespace Connected.Storage.PostgreSql;

/// <summary>
/// Provides PostgreSQL database writing capabilities for data modification operations.
/// </summary>
/// <remarks>
/// This class extends <see cref="DatabaseCommand"/> to implement <see cref="IStorageWriter"/>
/// for executing INSERT, UPDATE, and DELETE commands against PostgreSQL databases. It handles
/// command execution, transaction management, and connection lifecycle based on connection mode.
/// For Shared connections, the connection remains open for reuse; for Isolated connections,
/// the transaction is committed and the connection is closed after each operation. The writer
/// returns the number of rows affected by the command, which is useful for validating that
/// operations completed successfully. For INSERT operations with identity columns, the RETURNING
/// clause in PostgreSQL commands ensures generated values are retrieved. Connection and transaction
/// cleanup is performed automatically in finally blocks to ensure resources are released even
/// when exceptions occur.
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
	/// This method executes the storage operation's SQL command against the PostgreSQL database
	/// and returns the count of affected rows. For INSERT operations, PostgreSQL's RETURNING
	/// clause may be used to retrieve generated identity values. For UPDATE operations with
	/// optimistic concurrency, a return value of 0 indicates a concurrency conflict. For Isolated
	/// connection mode, the transaction is committed and the connection is closed after command
	/// execution. Connection cleanup is performed in a finally block to ensure resources are
	/// released even if an exception occurs during execution.
	/// </remarks>
	public async Task<int> Execute()
	{
		try
		{
			/*
			 * Execute command and get affected row count
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
			 * Cleanup isolated connections
			 */
			if (Connection.Mode == StorageConnectionMode.Isolated)
			{
				await Connection.Close();
				await Connection.DisposeAsync();
			}
		}
	}
}
