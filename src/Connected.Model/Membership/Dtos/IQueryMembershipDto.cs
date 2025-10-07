using Connected.Services;

namespace Connected.Membership.Dtos;

public interface IQueryMembershipDto : IDto
{
	string? Identity { get; set; }
	int? Role { get; set; }
}
