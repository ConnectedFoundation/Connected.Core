using Connected.Services;

namespace Connected.Membership.Dtos;

public interface IInsertMembershipDto : IDto
{
	string Identity { get; set; }
	int Role { get; set; }
}
