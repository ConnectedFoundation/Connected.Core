using Connected.Entities;

namespace Connected.Membership;

public interface IMembership : IEntity<long>
{
	string Identity { get; init; }
	int Role { get; init; }
}
