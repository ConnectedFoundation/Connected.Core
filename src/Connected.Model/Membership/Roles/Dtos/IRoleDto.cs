using Connected.Entities;
using Connected.Services;

namespace Connected.Membership.Roles.Dtos;

public interface IRoleDto : IDto
{
	string Name { get; set; }
	Status Status { get; set; }
	int? Parent { get; set; }
}
