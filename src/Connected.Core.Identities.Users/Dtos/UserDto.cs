using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Dtos;

internal abstract class UserDto : Dto, IUserDto
{
	[MaxLength(64)]
	public string? FirstName { get; set; }

	[MaxLength(64)]
	public string? LastName { get; set; }

	[MaxLength(1024)]
	public string? Email { get; set; }
}
