using Connected.Entities;
using System.Collections.Immutable;

namespace Connected.Storage;

public abstract class StorageConnectionProvider : Middleware, IStorageConnectionProvider
{
	protected IStorageContextDto Dto { get; private set; } = default!;

	public Task<IImmutableList<IStorageConnection>> Invoke<TEntity>(IStorageContextDto dto)
		where TEntity : IEntity
	{
		Dto = dto;

		return OnInvoke<TEntity>();
	}

	protected virtual Task<IImmutableList<IStorageConnection>> OnInvoke<TEntity>()
	{
		return Task.FromResult<IImmutableList<IStorageConnection>>(ImmutableList<IStorageConnection>.Empty);
	}
}