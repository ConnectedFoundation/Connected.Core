using Connected.Services;

namespace Connected.Identities.TwoFactor.Dtos;
/// <summary>
/// Completes a sign-in challenge with a code from the user's authenticator app.
/// </summary>
public interface IVerifyTwoFactorDto
	: IDto
{
	/// <summary>
	/// Gets or sets the challenge returned by the sign-in.
	/// </summary>
	string Challenge { get; set; }

	/// <summary>
	/// Gets or sets the six-digit code.
	/// </summary>
	string Code { get; set; }
}
