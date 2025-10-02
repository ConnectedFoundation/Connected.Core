using Connected.Entities;
using Connected.Identities;

namespace Connected.Membership.Roles;

public interface IRole : IEntity<int>, IIdentity
{
	string Name { get; init; }
	Status Status { get; init; }
	int? Parent { get; init; }
}
