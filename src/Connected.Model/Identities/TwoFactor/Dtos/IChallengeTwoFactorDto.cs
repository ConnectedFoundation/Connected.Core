using Connected.Services;

namespace Connected.Identities.TwoFactor.Dtos;
/// <summary>
/// Starts the second step of a user's sign-in.
/// </summary>
public interface IChallengeTwoFactorDto
	: IDto
{
	/// <summary>
	/// Gets or sets the id of the user.
	/// </summary>
	long User { get; set; }

	/// <summary>
	/// Gets or sets whether the authentication token issued at the end of the sign-in never expires.
	/// </summary>
	bool Permanent { get; set; }
}
