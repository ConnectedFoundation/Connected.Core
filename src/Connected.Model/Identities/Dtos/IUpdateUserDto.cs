using Connected.Services;

namespace Connected.Identities.Dtos;

/// <summary>
/// Represents a data transfer object for updating an existing user.
/// </summary>
/// <remarks>
/// This interface combines the base user properties with primary key identification
/// and status management to enable comprehensive user updates. The status property
/// allows control over the user's operational state.
/// </remarks>
public interface IUpdateUserDto
	: IUserDto, IPrimaryKeyDto<long>
{
	/// <summary>
	/// Gets or sets the operational status of the user.
	/// </summary>
	UserStatus Status { get; set; }
}