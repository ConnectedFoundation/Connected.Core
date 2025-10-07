using Connected.Identities.Dtos;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Users.Dtos;

internal sealed class InsertUserDto : UserDto, IInsertUserDto
{
	[MaxLength(256)]
	public string? Password { get; set; }
}
