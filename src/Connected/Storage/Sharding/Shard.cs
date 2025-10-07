using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Storage.Sharding;

[Table(SchemaAttribute.CoreSchema)]
internal sealed record Shard : ConcurrentEntity<int>, IShard
{
	private const string IndexName = $"idx_{SchemaAttribute.CoreSchema}_{nameof(Shard)}";

	[Ordinal(0)]
	public int Node { get; init; }

	[Ordinal(1), Length(1024), Index(true, IndexName)]
	public string Entity { get; init; } = default!;

	[Ordinal(2), Length(1024), Index(true, IndexName)]
	public string EntityId { get; init; } = default!;
}