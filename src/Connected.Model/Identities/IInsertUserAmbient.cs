using Connected.Identities.Dtos;
using Connected.Services;

namespace Connected.Identities;

/// <summary>
/// Provides ambient context for user insertion operations.
/// </summary>
/// <remarks>
/// This interface extends the ambient provider pattern to supply additional context
/// during user creation, including the security token and initial user status.
/// These ambient values can be used to enrich the insertion operation with contextual
/// information that may not be part of the standard DTO.
/// </remarks>
public interface IInsertUserAmbient
	: IAmbientProvider<IInsertUserDto>
{
	/// <summary>
	/// Gets or sets the security token for the user being inserted.
	/// </summary>
	string Token { get; set; }

	/// <summary>
	/// Gets or sets the initial status for the user being inserted.
	/// </summary>
	UserStatus Status { get; set; }
}
