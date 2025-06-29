using Connected.Membership.Roles.Dtos;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Roles;

internal sealed class InsertRoleAmbient : AmbientProvider<IInsertRoleDto>, IInsertRoleAmbient
{
	public InsertRoleAmbient()
	{
		Token = Guid.NewGuid().ToString();
	}

	[Required, MaxLength(128)]
	public required string Token { get; set; }
}
