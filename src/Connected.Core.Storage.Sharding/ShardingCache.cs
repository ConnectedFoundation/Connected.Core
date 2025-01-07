using Connected.Caching;

namespace Connected.Storage.Sharding;

internal sealed class ShardingCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<Shard, int>(cache, storage, MetaData.ShardingKey), IShardingCache
{
}