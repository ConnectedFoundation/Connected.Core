using Connected.Entities;

namespace Connected.Storage;

public abstract class StorageOperationProvider : Middleware, IStorageOperationProvider
{
	public async Task<IStorageOperation?> Invoke<TEntity>(IStorage<TEntity> storage, TEntity entity, IEnumerable<string>? updatingProperties)
			where TEntity : IEntity
	{
		return await OnInvoke(storage, entity, updatingProperties);
	}

	protected virtual async Task<IStorageOperation?> OnInvoke<TEntity>(IStorage<TEntity> storage, TEntity entity, IEnumerable<string>? updatingProperties)
		where TEntity : IEntity
	{
		return await Task.FromResult<IStorageOperation?>(null);
	}
}