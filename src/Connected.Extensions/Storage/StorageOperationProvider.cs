using Connected.Entities;

namespace Connected.Storage;

public abstract class StorageOperationProvider : Middleware, IStorageOperationProvider
{
	public Task<IStorageOperation?> Invoke<TEntity>(TEntity entity)
			where TEntity : IEntity
	{
		return OnInvoke(entity);
	}

	protected virtual Task<IStorageOperation?> OnInvoke<TEntity>(TEntity entity)
		where TEntity : IEntity
	{
		return Task.FromResult<IStorageOperation?>(null);
	}
}