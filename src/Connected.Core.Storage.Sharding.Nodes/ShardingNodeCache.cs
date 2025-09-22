using Connected.Caching;

namespace Connected.Storage.Sharding.Nodes;

internal sealed class ShardingNodeCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<IShardingNode, ShardingNode, int>(cache, storage, MetaData.ShardingNodeKey), IShardingNodeCache
{
}

