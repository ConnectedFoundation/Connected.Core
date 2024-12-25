using Connected.Entities;

namespace Connected.Storage;

public abstract class StorageConnectionProvider : MiddlewareComponent, IStorageConnectionProvider
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