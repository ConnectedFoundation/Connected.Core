using Connected.Annotations;

namespace Connected.Membership.Roles.Dtos;

internal sealed class UpdateRoleDto : RoleDto, IUpdateRoleDto
{
	[MinValue(1)]
	public int Id { get; set; }
}
