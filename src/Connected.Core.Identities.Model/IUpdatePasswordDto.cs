using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities;

public interface IUpdatePasswordDto : IPrimaryKeyDto<long>
{
	[MaxLength(256)]
	public string? Password { get; set; }

	[MaxLength(256)]
	public string? NewPassword { get; set; }
}