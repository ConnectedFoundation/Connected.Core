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
	/// The key is needed as soon as a user sets up two-factor sign-in. Changing or removing it makes every existing
	/// secret unreadable: those users cannot sign in, with credentials alone either, until their two-factor sign-in is
	/// reset (<c>ITwoFactorService.Reset</c>). They then sign in as users who have not set it up.
	/// </remarks>
	string? Key { get; }

	/// <summary>
	/// Gets whether every user must sign in with a code.
	/// </summary>
	/// <remarks>
	/// This covers every credential sign-in, Basic authentication included, so accounts that cannot enter a code, such
	/// as those of integrations, can no longer authenticate with credentials. Tokens issued before it was set stay valid
	/// until the user confirms two-factor sign-in, which revokes them; revoke them earlier to end such sessions at once.
	/// <para>
	/// A user who has not set up two-factor sign-in sets it up at the next credential sign-in, so whoever knows the
	/// password first can register their own authenticator. Applications that need to know the authenticator belongs to
	/// the account's owner enroll users through a channel that proves who they are, such as an invitation, before
	/// relying on it.
	/// </para>
	/// </remarks>
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
