using Connected.Entities;
using Connected.Storage.Sharding.Nodes;
using System.Collections.Immutable;

namespace Connected.Storage.Sharding;
/// <summary>
/// Represents a middleware which provides the sharding nodes for the specified entity.
/// </summary>
/// <typeparam name="TEntity">The entity type for which the middleware will provide the sharding nodes.</typeparam>
/// <remarks>
/// Sharding node provider does not provide directly shards but nodes which contain the data. It can return one
/// ore more nodes, but at least one node should always be returned. Default shard is always in the default storage
/// and exists even if sharding is not supported. This is the default behavior which means that shards can be 
/// added later as needed without affecting the existing nodes.
/// </remarks>
public interface IShardingNodeProvider<TEntity> : IMiddleware
	where TEntity : IEntity
{
	/// <summary>
	/// This method is called from the <c>IConnectionProvider</c> when a storage operation is requested
	/// by the <c>IServiceOperation</c>.
	/// </summary>
	/// <param name="operation">A storage operation which will be executed on all returned nodes. Operation is null
	/// in the startup when schema synchronization is executed. If the operation is null all shards (except the default)
	/// should be returned.</param>
	Task<ImmutableList<IShardingNode>> Invoke(IStorageOperation? operation);
}
