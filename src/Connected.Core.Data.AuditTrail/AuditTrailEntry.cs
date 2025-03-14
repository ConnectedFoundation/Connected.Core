using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Data.AuditTrail;
using Connected.Entities;

namespace Connected.Data;

internal sealed record AuditTrailEntry : EntityContainer<long>, IAuditTrail
{
	[Index(false, $"idx_{nameof(AuditTrailEntry)}_entity")]
	public override string Entity { get => base.Entity; init => base.Entity = value; }

	[Index(false, $"idx_{nameof(AuditTrailEntry)}_entity")]
	public override string EntityId { get => base.EntityId; init => base.EntityId = value; }

	[Index(false, $"idx_{nameof(AuditTrailEntry)}_created")]
	[Ordinal(0), Date(DateKind.DateTime2, 7)]
	public DateTimeOffset Created { get; init; }

	[Index(false, $"idx_{nameof(AuditTrailEntry)}_identity")]
	[Ordinal(1), Length(128)]
	public string? Identity { get; init; }

	[Ordinal(2), Length(128)]
	public string? Property { get; init; }

	[Ordinal(3), Length(1024)]
	public string? Value { get; init; }

	[Ordinal(4), Length(1024)]
	public string? Description { get; init; }

	[Ordinal(5)]
	public AuditTrailVerb Verb { get; init; }
}
