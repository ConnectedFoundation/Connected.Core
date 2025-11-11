using Connected.Services;

namespace Connected.Identities.Dtos;

/// <summary>
/// Represents a base data transfer object for user information.
/// </summary>
/// <remarks>
/// This interface defines the common user properties shared across various user
/// operations, including personal identification information such as name and email.
/// It serves as the foundational contract for user-related data transfer operations.
/// </remarks>
public interface IUserDto
	: IDto
{
	/// <summary>
	/// Gets or sets the user's first name.
	/// </summary>
	string? FirstName { get; set; }

	/// <summary>
	/// Gets or sets the user's last name.
	/// </summary>
	string? LastName { get; set; }

	/// <summary>
	/// Gets or sets the user's email address.
	/// </summary>
	string? Email { get; set; }
}
