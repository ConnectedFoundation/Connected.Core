using Connected.Entities;

namespace Connected.Membership.Claims;

public record ClaimSchema : Entity, IClaimSchema
{
	public required string Text { get; init; }
	public required string Entity { get; init; }
	public required string EntityId { get; init; }
}
