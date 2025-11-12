namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Base class for schema synchronization queries that retrieve information from the database.
/// </summary>
/// <typeparam name="T">The type of result returned by the query.</typeparam>
/// <remarks>
/// This abstract class provides the foundation for all schema query operations such as checking
/// table existence, retrieving column metadata, or querying constraints. It extends
/// <see cref="SynchronizationCommand"/> and manages the execution context containing connection
/// information and schema metadata. The Execute method initializes the context and delegates to
/// the derived class's OnExecute implementation to perform the actual query operation. Unlike
/// <see cref="SynchronizationTransaction"/>, query operations return a typed result rather than
/// just completing successfully, enabling schema discovery and comparison operations.
/// </remarks>
internal abstract class SynchronizationQuery<T>
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
	/// Executes the synchronization query with the specified context.
	/// </summary>
	/// <param name="context">The execution context containing connection and schema information.</param>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains the query result of type <typeparamref name="T"/>.
	/// </returns>
	/// <remarks>
	/// This method initializes the execution context and invokes the derived class's OnExecute
	/// implementation to perform the actual query operation and return the result.
	/// </remarks>
	public async Task<T> Execute(SchemaExecutionContext context)
	{
		Context = context;

		return await OnExecute();
	}

	/// <summary>
	/// Performs the actual schema query operation.
	/// </summary>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains the query result of type <typeparamref name="T"/>.
	/// </returns>
	/// <remarks>
	/// Derived classes must implement this method to perform specific query logic such as
	/// checking table existence, retrieving column definitions, or querying index metadata.
	/// </remarks>
	protected abstract Task<T> OnExecute();
}
