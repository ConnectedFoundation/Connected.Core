namespace Connected.Storage;

public abstract class StorageWriterProvider : Middleware, IStorageWriterProvider
{
	protected IStorageOperation Operation { get; private set; } = default!;
	protected IStorageConnection Connection { get; private set; } = default!;

	public Task<IStorageWriter?> Invoke<TEntity>(IStorageOperation operation, IStorageConnection connection)
	{
		Operation = operation;
		Connection = connection;

		return OnInvoke<TEntity>();
	}

	protected virtual Task<IStorageWriter?> OnInvoke<TEntity>()
	{
		return Task.FromResult<IStorageWriter?>(null);
	}
}