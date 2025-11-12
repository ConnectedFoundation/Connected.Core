using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage;

public interface IStorageConnectionProvider : IMiddleware
{
	Task<IImmutableList<IStorageConnection>> Invoke<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity;
}