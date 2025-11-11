namespace Connected.Identities.Dtos;

/// <summary>
/// Represents a data transfer object for inserting a new user.
/// </summary>
/// <remarks>
/// This interface extends the base user DTO with an additional password property
/// required for user creation. The password is optional to support scenarios
/// where authentication may be handled externally.
/// </remarks>
public interface IInsertUserDto
	: IUserDto
{
	/// <summary>
	/// Gets or sets the user's password for authentication.
	/// </summary>
	string? Password { get; set; }
}