using Connected.Caching;

namespace Connected.Storage.Sharding.Nodes;

internal sealed class ShardingNodeCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<ShardingNode, int>(cache, storage, MetaData.ShardingNodeKey), IShardingNodeCache
{
}

