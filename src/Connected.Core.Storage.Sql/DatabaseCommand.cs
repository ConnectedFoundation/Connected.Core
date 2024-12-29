namespace Connected.Storage.Sql;

internal abstract class DatabaseCommand(IStorageOperation operation, IStorageConnection connection) : IStorageCommand
{
	private IStorageConnection? _connection = connection;

	protected bool IsDisposed { get; private set; }
	public IStorageOperation Operation { get; } = operation;
	public IStorageConnection Connection
	{
		get
		{
			if (_connection is null)
				throw new NullReferenceException("Connection is null");

			return _connection;
		}
	}

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

	protected virtual async ValueTask OnDisposingAsync()
	{
		await ValueTask.CompletedTask;
	}

	protected virtual void OnDisposing()
	{
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(true);
		GC.SuppressFinalize(this);
	}

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

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
