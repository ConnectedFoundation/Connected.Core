namespace Connected.Storage;

public interface IQueryMiddleware : IQueryProvider, IMiddleware
{
	Task<bool> Invoke(Type entityType, StorageConnectionMode connectionMode);
}
