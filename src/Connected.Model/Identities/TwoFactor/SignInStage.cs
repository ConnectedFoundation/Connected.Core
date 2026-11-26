namespace Connected.Identities.TwoFactor;
/// <summary>
/// Describes what a sign-in still needs.
/// </summary>
public enum SignInStage
{
	/// <summary>
	/// The sign-in is complete and carries an authentication token.
	/// </summary>
	Completed = 0,

	/// <summary>
	/// The sign-in carries a challenge that is completed with a code from the user's authenticator app.
	/// </summary>
	TwoFactor = 1,

	/// <summary>
	/// The user must set up two-factor sign-in first; the sign-in carries a challenge that returns a new secret and is
	/// completed with the secret's first code.
	/// </summary>
	TwoFactorSetup = 2
}
