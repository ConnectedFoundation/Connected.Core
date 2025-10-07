using Connected.Membership.Roles.Dtos;

namespace Connected.Core.Membership.Mock.Roles.Dtos;
public class UpdateRoleDtoMock : RoleDtoMock, IUpdateRoleDto
{
	public int Id { get; set; }
}
