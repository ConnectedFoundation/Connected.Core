using Connected.Annotations;
using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Membership.Claims;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed record Claim : Entity<long>, IClaim
{
	[Ordinal(0), Length(256)]
	public string? Schema { get; init; }

	[Ordinal(1), Length(256)]
	public string? Identity { get; init; }

	[Ordinal(2), Length(256)]
	public string? Type { get; init; }

	[Ordinal(3), Length(256)]
	public string? PrimaryKey { get; init; }

	[Ordinal(4), Length(256)]
	public ClaimStatus Status { get; init; }

	[Ordinal(5), Length(256)]
	public required string Value { get; init; }
}
