using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Storage.Sharding.Nodes;

[Table(SchemaAttribute.CoreSchema)]
internal sealed record ShardingNode : Entity<int>, IShardingNode
{
	[Ordinal(0), Length(128)]
	public string Name { get; init; } = default!;

	[Ordinal(1), Length(1024), Index(true)]
	public string ConnectionString { get; init; } = default!;

	[Ordinal(2)]
	public Status Status { get; init; }
}