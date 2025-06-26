using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Dtos;

public interface IUpdatePasswordDto : IPrimaryKeyDto<long>
{
	[MaxLength(256)]
	public string? Password { get; set; }
}