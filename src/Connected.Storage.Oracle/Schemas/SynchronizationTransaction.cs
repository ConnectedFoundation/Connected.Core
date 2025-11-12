namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Base class for Oracle schema synchronization transactions that perform database modifications.
/// </summary>
/// <remarks>
/// This abstract class provides the foundation for all schema modification operations such as
/// creating, altering, or dropping database objects in Oracle databases. It extends <see cref="SynchronizationCommand"/>
/// and manages the execution context containing connection information, schema metadata, and
/// constraint registries. The Execute method initializes the context and delegates to the
/// derived class's OnExecute implementation to perform the actual schema operation. This pattern
/// ensures consistent context management and error handling across all schema modification
/// transactions. Oracle-specific considerations include automatic DDL commits, uppercase identifier
/// handling, and constraint name length limits (30 characters pre-12.2, 128 characters 12.2+).
/// </remarks>
internal abstract class SynchronizationTransaction
	: SynchronizationCommand
{
	/// <summary>
	/// Gets the execution context for the schema operation.
	/// </summary>
	/// <value>
	/// The <see cref="SchemaExecutionContext"/> providing access to connection resources and metadata.
	/// </value>
	protected SchemaExecutionContext Context { get; private set; } = default!;

	/// <summary>
	/// Executes the synchronization transaction with the specified context.
	/// </summary>
	/// <param name="context">The execution context containing connection and schema information.</param>
	/// <returns>A task representing the asynchronous execution operation.</returns>
	/// <remarks>
	/// This method initializes the execution context and invokes the derived class's OnExecute
	/// implementation to perform the actual schema modification operation. Oracle automatically
	/// commits DDL statements, so transactions cannot be rolled back once executed.
	/// </remarks>
	public async Task Execute(SchemaExecutionContext context)
	{
		Context = context;

		await OnExecute();
	}

	/// <summary>
	/// Performs the actual schema synchronization operation.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// Derived classes override this method to implement specific schema modification logic
	/// such as creating tables, adding columns, modifying constraints, or creating indexes.
	/// Oracle DDL operations are auto-committed and cannot be rolled back.
	/// </remarks>
	protected virtual async Task OnExecute()
	{
		await Task.CompletedTask;
	}
}
