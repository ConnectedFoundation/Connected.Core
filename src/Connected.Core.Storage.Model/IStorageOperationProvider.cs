using Connected.Entities;

namespace Connected.Storage;

public interface IStorageOperationProvider : IMiddleware
{
	Task<IStorageOperation?> Invoke<TEntity>(TEntity entity)
		where TEntity : IEntity;
}