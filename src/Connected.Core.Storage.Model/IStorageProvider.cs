using Connected.Entities;

namespace Connected.Storage;

public interface IStorageProvider
{
	IStorage<TEntity> Open<TEntity>()
		where TEntity : IEntity;

	IStorage<TEntity> Open<TEntity>(StorageConnectionMode mode)
		where TEntity : IEntity;
}
