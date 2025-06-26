using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Dtos;

internal sealed class UpdatePasswordDto : Dto, IUpdatePasswordDto
{
	[MaxLength(256)]
	public string? Password { get; set; }

	[MinValue(1)]
	public long Id { get; set; }
}
