namespace Connected.Storage;

public interface IStorageWriterProvider : IMiddleware
{
	Task<IStorageWriter?> Invoke<TEntity>(IStorageOperation operation, IStorageConnection connection);
}