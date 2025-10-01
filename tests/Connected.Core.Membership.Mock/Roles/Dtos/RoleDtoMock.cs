using Connected.Core.Services.Mock;
using Connected.Entities;
using Connected.Membership.Roles.Dtos;

namespace Connected.Core.Membership.Mock.Roles.Dtos;
public class RoleDtoMock : DtoMock, IRoleDto
{
	public required string Name { get; set; }
	public Status Status { get; set; }
	public int? Parent { get; set; }
}
