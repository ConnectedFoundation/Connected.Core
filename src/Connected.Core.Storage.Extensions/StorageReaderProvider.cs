namespace Connected.Storage;

public abstract class StorageReaderProvider : Middleware, IStorageReaderProvider
{
	protected IStorageOperation Operation { get; private set; } = default!;
	protected IStorageConnection Connection { get; private set; } = default!;

	public Task<IStorageReader<TEntity>?> Invoke<TEntity>(IStorageOperation operation, IStorageConnection connection)
	{
		Operation = operation;
		Connection = connection;

		return OnInvoke<TEntity>();
	}

	protected virtual Task<IStorageReader<TEntity>?> OnInvoke<TEntity>()
	{
		return Task.FromResult<IStorageReader<TEntity>?>(null);
	}
}
