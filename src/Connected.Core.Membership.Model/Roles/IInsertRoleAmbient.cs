using Connected.Membership.Roles.Dtos;
using Connected.Services;

namespace Connected.Membership.Roles;

public interface IInsertRoleAmbient : IAmbientProvider<IInsertRoleDto>
{
	string Token { get; set; }
}
