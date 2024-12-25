using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage;

public interface IStorageConnectionProvider : IMiddleware
{
	Task<ImmutableList<IStorageConnection>> Invoke<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity;
}