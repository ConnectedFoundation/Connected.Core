using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Identities.Dtos;

/// <summary>
/// Represents a data transfer object for updating a user's password.
/// </summary>
/// <remarks>
/// This interface combines primary key identification with password data to enable
/// secure password updates for existing users. The password field is constrained
/// to a maximum length of 256 characters.
/// </remarks>
public interface IUpdatePasswordDto
	: IPrimaryKeyDto<long>
{
	/// <summary>
	/// Gets or sets the new password value.
	/// </summary>
	[MaxLength(256)]
	public string? Password { get; set; }
}