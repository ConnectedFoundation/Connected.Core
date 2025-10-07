using Connected.Entities;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Membership.Roles.Dtos;

internal abstract class RoleDto : Dto, IRoleDto
{
	[Required, MaxLength(128)]
	public required string Name { get; set; }

	public Status Status { get; set; }

	public int? Parent { get; set; }
}
