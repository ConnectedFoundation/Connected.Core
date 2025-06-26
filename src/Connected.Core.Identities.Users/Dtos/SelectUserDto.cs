using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Dtos;

internal sealed class SelectUserDto : Dto, ISelectUserDto
{
	[MaxLength(1024)]
	public required string User { get; set; }

	[MaxLength(256)]
	public string? Password { get; set; }
}
