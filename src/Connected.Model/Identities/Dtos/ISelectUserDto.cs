using Connected.Services;

namespace Connected.Identities.Dtos;

/// <summary>
/// Represents a data transfer object for selecting and authenticating a user.
/// </summary>
/// <remarks>
/// This interface provides credentials for user selection and authentication operations,
/// combining user identification with optional password verification to support
/// various authentication scenarios.
/// </remarks>
public interface ISelectUserDto
	: IDto
{
	/// <summary>
	/// Gets or sets the user identifier for selection.
	/// </summary>
	string User { get; set; }

	/// <summary>
	/// Gets or sets the password for authentication verification.
	/// </summary>
	string? Password { get; set; }
}