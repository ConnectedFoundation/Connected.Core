namespace Connected.Identities.TwoFactor;
/// <summary>
/// The outcome of a sign-in with valid credentials.
/// </summary>
public sealed class SignInResult
{
	/// <summary>
	/// Gets what the sign-in still needs.
	/// </summary>
	public SignInStage Stage { get; init; }

	/// <summary>
	/// Gets the authentication token when the sign-in is <see cref="SignInStage.Completed"/>.
	/// </summary>
	public string? Token { get; init; }

	/// <summary>
	/// Gets the challenge that continues the sign-in when a code is needed.
	/// </summary>
	/// <remarks>
	/// The challenge is a secret of the sign-in; only its hash is stored.
	/// </remarks>
	public string? Challenge { get; init; }

	/// <summary>
	/// Gets the time the challenge expires, in UTC.
	/// </summary>
	public DateTimeOffset? Expire { get; init; }
}
