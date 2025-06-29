using Connected.Entities;

namespace Connected.Membership.Claims;

public record ClaimSchema : Entity<string>, IClaimSchema
{
	public required string Text { get; init; }
	public required string Type { get; init; }
}
