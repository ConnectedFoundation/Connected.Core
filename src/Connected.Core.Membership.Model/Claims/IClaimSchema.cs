using Connected.Entities;

namespace Connected.Membership.Claims;

public interface IClaimSchema : IEntity<string>
{
	string Text { get; init; }
	string Type { get; init; }
}
