using Connected.Caching;

namespace Connected.Storage.Sharding;
internal interface IShardingCache : IEntityCache<IShard, int>
{

}
