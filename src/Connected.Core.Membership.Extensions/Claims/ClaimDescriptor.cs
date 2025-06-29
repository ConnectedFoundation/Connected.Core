using Connected.Entities;

namespace Connected.Membership.Claims;

public record ClaimDescriptor : Entity<string>, IClaimDescriptor
{
	public required string Text { get; init; }
}
