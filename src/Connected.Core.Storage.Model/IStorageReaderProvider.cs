namespace Connected.Storage;

public interface IStorageReaderProvider : IMiddleware
{
	Task<IStorageReader<TEntity>?> Invoke<TEntity>(IStorageOperation operation, IStorageConnection connection);
}