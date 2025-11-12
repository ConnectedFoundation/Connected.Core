using Connected.Entities;

namespace Connected.Membership.Claims;

public interface IClaimDescriptor : IEntity
{
	string Entity { get; init; }
	string EntityId { get; init; }
	string Value { get; init; }
	string? Text { get; init; }
}
