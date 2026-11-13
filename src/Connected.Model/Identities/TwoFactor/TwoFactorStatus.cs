namespace Connected.Identities.TwoFactor;
/// <summary>
/// Describes whether a user has set up two-factor sign-in.
/// </summary>
public enum TwoFactorStatus
{
	/// <summary>
	/// The user has no secret.
	/// </summary>
	None = 0,

	/// <summary>
	/// The user has a secret that is not confirmed with a code yet.
	/// </summary>
	Pending = 1,

	/// <summary>
	/// The user signs in with a code.
	/// </summary>
	Enabled = 2
}
