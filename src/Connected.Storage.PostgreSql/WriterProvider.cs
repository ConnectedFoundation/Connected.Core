using Connected.Annotations;

namespace Connected.Storage.PostgreSql;

/// <summary>
/// Provides PostgreSQL database writer instances for data modification operations.
/// </summary>
/// <remarks>
/// This sealed class implements <see cref="StorageWriterProvider"/> to create writer instances
/// for executing INSERT, UPDATE, and DELETE commands against PostgreSQL databases. It creates
/// <see cref="DatabaseWriter"/> instances configured with the current operation and connection.
/// The writer handles command execution, transaction management, and connection lifecycle based
/// on connection mode (Shared or Isolated). Priority 0 ensures writers are created before other
/// providers in the middleware pipeline. The writer returns the number of affected rows and
/// properly manages PostgreSQL-specific features like RETURNING clauses for identity values.
/// </remarks>
[Priority(0)]
internal sealed class WriterProvider
	: StorageWriterProvider
{
	/// <summary>
	/// Creates a storage writer for the specified entity type.
	/// </summary>
	/// <typeparam name="TEntity">The entity type to create the writer for.</typeparam>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// the <see cref="IStorageWriter"/> for executing commands, or <c>null</c> if creation fails.
	/// </returns>
	/// <remarks>
	/// This method creates a <see cref="DatabaseWriter"/> configured with the current storage
	/// operation and connection. The writer is responsible for executing DML commands (INSERT,
	/// UPDATE, DELETE), managing transactions, and handling connection lifecycle based on the
	/// connection mode. For isolated connections, the writer commits transactions and closes
	/// connections after command execution. The writer returns the number of rows affected by
	/// the operation.
	/// </remarks>
	protected override Task<IStorageWriter?> OnInvoke<TEntity>()
	{
		return Task.FromResult<IStorageWriter?>(new DatabaseWriter(Operation, Connection));
	}
}
