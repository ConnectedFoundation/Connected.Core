namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Base class for PostgreSQL schema synchronization query operations that retrieve data.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query operation.</typeparam>
/// <remarks>
/// This abstract class provides the foundation for all schema query operations that read
/// information from the database without modifying it. It extends <see cref="SynchronizationCommand"/>
/// and manages the execution context for query operations. The Execute method initializes the
/// context and delegates to the derived class's OnExecute implementation to perform the actual
/// query and return results. This pattern ensures consistent context management across all
/// schema query operations.
/// </remarks>
internal abstract class SynchronizationQuery<TResult>
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
	/// A task representing the asynchronous execution operation. The task result contains
	/// the query result of type <typeparamref name="TResult"/>.
	/// </returns>
	/// <remarks>
	/// This method initializes the execution context and invokes the derived class's OnExecute
	/// implementation to perform the actual query operation and retrieve results.
	/// </remarks>
	public async Task<TResult> Execute(SchemaExecutionContext context)
	{
		Context = context;

		return await OnExecute();
	}

	/// <summary>
	/// Performs the actual schema query operation.
	/// </summary>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains
	/// the query result of type <typeparamref name="TResult"/>.
	/// </returns>
	/// <remarks>
	/// Derived classes override this method to implement specific schema query logic
	/// such as checking existence, retrieving metadata, or querying schema information.
	/// </remarks>
	protected abstract Task<TResult> OnExecute();
}
