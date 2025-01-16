using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage;

public abstract class StorageConnectionProvider : Middleware, IStorageConnectionProvider
{
	protected IStorageContextDto Dto { get; private set; } = default!;

	public Task<ImmutableList<IStorageConnection>> Invoke<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity
	{
		Dto = dto;

		return OnInvoke<TEntity>();
	}

	protected virtual Task<ImmutableList<IStorageConnection>> OnInvoke<TEntity>()
	{
		return Task.FromResult(ImmutableList<IStorageConnection>.Empty);
	}
}