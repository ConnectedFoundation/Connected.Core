using Connected.Entities;

namespace Connected.Membership.Roles;

public interface IRole : IEntity<int>
{
	string Name { get; init; }
	Status Status { get; init; }
	int? Parent { get; init; }
}
