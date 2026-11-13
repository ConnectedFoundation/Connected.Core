namespace Connected.Identities.TwoFactor;
/// <summary>
/// A new two-factor secret for the user's authenticator app.
/// </summary>
/// <remarks>
/// The secret is returned only while it is set up; afterwards it is stored encrypted and never returned.
/// </remarks>
public sealed class TwoFactorSetup
{
	/// <summary>
	/// Gets the secret, encoded in Base32, for entering it by hand.
	/// </summary>
	public required string Secret { get; init; }

	/// <summary>
	/// Gets the <c>otpauth://totp</c> URI of the secret, for a QR code.
	/// </summary>
	public required string Uri { get; init; }
}
