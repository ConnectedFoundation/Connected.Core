using Connected.Entities;

namespace Connected.Membership.Claims;

public interface IClaimDescriptor : IEntity<string>
{
	string Text { get; init; }
}
