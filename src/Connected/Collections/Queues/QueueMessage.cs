using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Collections.Queues.Serialization;
using Connected.Entities;
using Connected.Services;
using System.Data;

namespace Connected.Collections.Queues;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record QueueMessage : ConcurrentEntity<long>, IQueueMessage
{
	[Ordinal(0), Date(DateKind.DateTime)]
	public DateTimeOffset Created { get; init; }

	[Ordinal(1)]
	public int DequeueCount { get; init; }

	[Ordinal(2), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset? DequeueTimestamp { get; init; }

	[Ordinal(3), Length(1024)]
	public string DtoTypeName { get; init; } = default!;

	[Ordinal(4), Length(1024), DataType(DbType.Binary)]
	[Serializer(typeof(DtoSerializer))]
	public IDto Dto { get; init; } = default!;

	[Ordinal(5), Length(1024), DataType(DbType.String)]
	[Serializer(typeof(TypeSerializer))]
	public Type Client { get; init; } = default!;

	[Ordinal(6), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset NextVisible { get; init; }

	[Ordinal(7)]
	public Guid? PopReceipt { get; init; }

	[Ordinal(8), Length(256)]
	public string? Batch { get; init; }

	[Ordinal(9), Date(DateKind.DateTime)]
	public DateTimeOffset Expire { get; init; } = default!;

	[Ordinal(10)]
	public int Priority { get; init; } = default!;

	[Ordinal(11), Length(128)]
	public string Queue { get; init; } = default!;

	[Ordinal(12)]
	public int MaxDequeueCount { get; init; }
}
