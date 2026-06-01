using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Storage.Sharding.Nodes;

[Table(SchemaAttribute.CoreSchema)]
/*
 * Priority must be above the sharding entities because this entity must be created first
 * so sharding entities can technically pass successfuly (even if it's empty on first run, 
 * but it would fail if the entity is not created yet because sharding entities typicaly
 * performs query on sharding nodes)
 */
[Priority(10)]
internal sealed record ShardingNode : Entity<int>, IShardingNode
{
	[Ordinal(0), Length(128)]
	public string Name { get; init; } = default!;

	[Ordinal(1), Length(1024), Index(true)]
	public string ConnectionString { get; init; } = default!;

	[Ordinal(2)]
	public Status Status { get; init; }
}