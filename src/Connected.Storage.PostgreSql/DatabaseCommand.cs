namespace Connected.Storage.PostgreSql;

/// <summary>
/// Provides base functionality for PostgreSQL database command execution.
/// </summary>
/// <remarks>
/// This abstract class serves as the foundation for database reader and writer implementations,
/// managing the lifecycle of storage operations and connections. It implements both synchronous
/// and asynchronous disposal patterns to ensure proper resource cleanup. The class holds references
/// to the storage operation (containing SQL command text and parameters) and the database connection
/// (managing the Npgsql connection). Derived classes implement specific command execution patterns
/// for reading (SELECT) or writing (INSERT/UPDATE/DELETE) operations. The disposal pattern ensures
/// connections are properly released and resources are cleaned up even in error scenarios.
/// </remarks>
internal abstract class DatabaseCommand(IStorageOperation operation, IStorageConnection connection) : IStorageCommand
{
	private IStorageConnection? _connection = connection;

	/// <summary>
	/// Gets a value indicating whether the command has been disposed.
	/// </summary>
	protected bool IsDisposed { get; private set; }

	/// <summary>
	/// Gets the storage operation containing the SQL command and parameters.
	/// </summary>
	/// <value>
	/// The <see cref="IStorageOperation"/> with command text, parameters, and execution settings.
	/// </value>
	public IStorageOperation Operation { get; } = operation;

	/// <summary>
	/// Gets the database connection for command execution.
	/// </summary>
	/// <value>
	/// The <see cref="IStorageConnection"/> managing the PostgreSQL database connection.
	/// </value>
	/// <exception cref="NullReferenceException">Thrown when the connection has been disposed.</exception>
	public IStorageConnection Connection
	{
		get
		{
			if (_connection is null)
				throw new NullReferenceException("Connection is null");

			return _connection;
		}
	}

	/// <summary>
	/// Asynchronously disposes the command and releases associated resources.
	/// </summary>
	/// <param name="disposing">
	/// <c>true</c> to release both managed and unmanaged resources;
	/// <c>false</c> to release only unmanaged resources.
	/// </param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
	/// <remarks>
	/// This method clears the connection reference and calls <see cref="OnDisposingAsync"/>
	/// for derived class cleanup. It follows the async disposal pattern to properly release
	/// resources that may require async operations during cleanup.
	/// </remarks>
	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				_connection = null;

				await OnDisposingAsync();
			}

			IsDisposed = true;
		}
	}

	/// <summary>
	/// Performs asynchronous cleanup operations for derived classes.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous cleanup operation.</returns>
	/// <remarks>
	/// Derived classes can override this method to perform custom async cleanup operations
	/// such as closing readers or releasing database-specific resources. The base implementation
	/// returns a completed task.
	/// </remarks>
	protected virtual async ValueTask OnDisposingAsync()
	{
		await ValueTask.CompletedTask;
	}

	/// <summary>
	/// Performs synchronous cleanup operations for derived classes.
	/// </summary>
	/// <remarks>
	/// Derived classes can override this method to perform custom sync cleanup operations.
	/// The base implementation is empty. Prefer <see cref="OnDisposingAsync"/> for async
	/// resource cleanup when possible.
	/// </remarks>
	protected virtual void OnDisposing()
	{
	}

	/// <summary>
	/// Asynchronously disposes the command and suppresses finalization.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous disposal operation.</returns>
	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Synchronously disposes the command and releases associated resources.
	/// </summary>
	/// <param name="disposing">
	/// <c>true</c> to release both managed and unmanaged resources;
	/// <c>false</c> to release only unmanaged resources.
	/// </param>
	/// <remarks>
	/// This method clears the connection reference and calls <see cref="OnDisposing"/>
	/// for derived class cleanup. It follows the standard .NET disposal pattern.
	/// </remarks>
	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				_connection = null;

				OnDisposing();
			}

			IsDisposed = true;
		}
	}

	/// <summary>
	/// Synchronously disposes the command and suppresses finalization.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
