using Connected.Annotations;
using Connected.Identities.Dtos;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Users.Dtos;

internal sealed class UpdatePasswordDto : Dto, IUpdatePasswordDto
{
	[MaxLength(256)]
	public string? Password { get; set; }

	[MinValue(1)]
	public long Id { get; set; }
}
