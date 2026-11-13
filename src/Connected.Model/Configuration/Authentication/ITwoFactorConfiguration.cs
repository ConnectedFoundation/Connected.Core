namespace Connected.Configuration.Authentication;
/// <summary>
/// Configuration contract of two-factor sign-in with time-based one-time passwords (RFC 6238).
/// </summary>
/// <remarks>
/// Two-factor sign-in is off until it is required or a user sets it up. Users who have set it up always sign in with
/// a code; <see cref="Required"/> makes every user set it up at their next sign-in.
/// </remarks>
public interface ITwoFactorConfiguration
{
	/// <summary>
	/// The number of minutes a sign-in challenge stays valid when none is configured.
	/// </summary>
	const int DefaultChallengeLifetime = 10;

	/// <summary>
	/// The number of wrong codes in a row a user may enter when none is configured.
	/// </summary>
	const int DefaultMaxAttempts = 5;

	/// <summary>
	/// The number of minutes a user's codes are refused after too many wrong ones when none is configured.
	/// </summary>
	const int DefaultLockoutDuration = 15;

	/// <summary>
	/// Gets the 256-bit key, encoded in Base64, that encrypts the users' secrets with AES-GCM.
	/// </summary>
	/// <remarks>
	/// The key is needed as soon as a user sets up two-factor sign-in. Changing it makes every existing secret
	/// unreadable, so those users must set up two-factor sign-in again.
	/// </remarks>
	string? Key { get; }

	/// <summary>
	/// Gets whether every user must sign in with a code.
	/// </summary>
	bool Required { get; }

	/// <summary>
	/// Gets the issuer shown by authenticator apps next to the account.
	/// </summary>
	string? Issuer { get; }

	/// <summary>
	/// Gets the number of minutes a sign-in challenge stays valid.
	/// </summary>
	int ChallengeLifetime { get; }

	/// <summary>
	/// Gets the number of wrong codes in a row after which a user's codes are refused for <see cref="LockoutDuration"/>.
	/// </summary>
	/// <remarks>
	/// The codes are counted per user, across sign-ins and challenges, so signing in again does not give more attempts.
	/// A right code resets the count.
	/// </remarks>
	int MaxAttempts { get; }

	/// <summary>
	/// Gets the number of minutes a user's codes are refused after <see cref="MaxAttempts"/> wrong codes in a row.
	/// </summary>
	int LockoutDuration { get; }
}
