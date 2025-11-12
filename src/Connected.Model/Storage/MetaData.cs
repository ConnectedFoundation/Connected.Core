using Connected.Annotations.Entities;
using Connected.Storage.Sharding;
using Connected.Storage.Sharding.Nodes;

namespace Connected.Storage;
public static class MetaData
{
	public const string ShardingKey = $"{SchemaAttribute.CoreSchema}.{nameof(IShard)}";
	public const string ShardingNodeKey = $"{SchemaAttribute.CoreSchema}.{nameof(IShardingNode)}";
}
