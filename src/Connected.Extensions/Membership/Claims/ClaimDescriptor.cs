using Connected.Entities;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Claims;

public record ClaimDescriptor : Entity, IClaimDescriptor
{
	[MaxLength(256)]
	public string? Text { get; init; }

	[Required, MaxLength(256)]
	public required string Entity { get; init; }

	[Required, MaxLength(256)]
	public required string EntityId { get; init; }

	[Required, MaxLength(256)]
	public required string Value { get; init; }
}
