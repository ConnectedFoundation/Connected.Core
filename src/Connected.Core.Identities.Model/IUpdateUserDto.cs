using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities;

public interface IUpdateUserDto : IPrimaryKeyDto<long>
{
	[MaxLength(32)]
	public string? FirstName { get; set; }

	[MaxLength(64)]
	public string? LastName { get; set; }

	[MaxLength(256)]
	public string? Email { get; set; }

	public UserStatus Status { get; set; }
}