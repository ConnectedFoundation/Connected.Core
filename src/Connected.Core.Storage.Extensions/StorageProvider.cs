using Connected.Entities;
using Connected.Entities.Protection;
using Connected.Storage.Transactions;

namespace Connected.Storage;

internal sealed class StorageProvider(IEntityProtectionService dataProtection, IConnectionProvider connections, ITransactionContext transactions, IMiddlewareService middleware)
	: IStorageProvider
{
	/// <summary>
	/// Opens <see cref="IDatabaseContext{TEntity}"/> for reading and writing entities.
	/// </summary>
	/// <typeparam name="TEntity">Type type of the entity to be used.</typeparam>
	/// <returns>The <see cref="IDatabaseContext{TEntity}"/> on which LINQ queries and updates can be performed.</returns>
	public IStorage<TEntity> Open<TEntity>()
		where TEntity : IEntity
	{
		return Open<TEntity>(StorageConnectionMode.Shared);
	}

	public IStorage<TEntity> Open<TEntity>(StorageConnectionMode mode)
		where TEntity : IEntity
	{
		var result = new EntityStorage<TEntity>(dataProtection, middleware, connections, transactions, mode);

		result.Initialize().Wait();

		return result;
	}
}
