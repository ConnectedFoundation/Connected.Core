using Connected.Entities;
using Connected.Storage.Sharding;
using Connected.Storage.Sharding.Nodes;
using System.Collections.Immutable;

namespace Connected.Storage;
/// <summary>
/// A default implementation of the IShardingNodeProvider.
/// </summary>
public abstract class ShardingNodeProvider<TEntity> : MiddlewareComponent, IShardingNodeProvider<TEntity>
	where TEntity : IEntity
{
	/// <summary>
	/// gets the operation the will be executed on all returned nodes.
	/// <summary>
	protected IStorageOperation? Operation { get; private set; }
	/// <inheritdoc cref="IShardingNodeProvider{TEntity}.Invoke"/>>
	public async Task<ImmutableList<IShardingNode>> Invoke(IStorageOperation? operation)
	{
		Operation = operation;

		return await OnInvoke();
	}
	/// <summary>
	/// This method should return at least one <c>IShardingNode</c> for the specified Operation.
	/// </summary>
	protected abstract Task<ImmutableList<IShardingNode>> OnInvoke();
}