using Connected.Entities;

namespace Connected.Storage;

public interface IStorageConnectionProvider : IMiddleware
{
	Task<ImmutableList<IStorageConnection>> Invoke<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity;
}