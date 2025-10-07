using Connected.Entities;

namespace Connected.Membership.Claims;

public interface IClaimSchema : IEntity
{
	string Text { get; init; }
	string Entity { get; init; }
	string EntityId { get; init; }
}
