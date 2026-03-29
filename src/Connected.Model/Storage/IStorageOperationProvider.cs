using Connected.Entities;

namespace Connected.Storage;

public interface IStorageOperationProvider : IMiddleware
{
	Task<IStorageOperation?> Invoke<TEntity>(IStorage<TEntity> storage, TEntity entity, IEnumerable<string>? updatingProperties)
		where TEntity : IEntity;
}