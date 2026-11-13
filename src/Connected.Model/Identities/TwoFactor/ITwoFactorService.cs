using Connected.Annotations;
using Connected.Identities.TwoFactor.Dtos;
using Connected.Services;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Provides two-factor sign-in with time-based one-time passwords (RFC 6238: HMAC-SHA1, six digits, 30 seconds).
/// </summary>
/// <remarks>
/// <para>
/// A sign-in with valid credentials returns a challenge instead of a token when the user signs in with a code. The
/// challenge is completed with <see cref="Verify"/>, which issues the authentication token. A user who must set up
/// two-factor sign-in first gets a setup challenge: <see cref="Setup"/> returns a new secret for it, and the secret's
/// first code completes the sign-in and enables two-factor sign-in.
/// </para>
/// <para>
/// <see cref="Setup"/> and <see cref="Verify"/> are open to anonymous callers, since the challenge is their credential.
/// The other operations are not exposed over the network.
/// </para>
/// </remarks>
[Service, ServiceUrl(IdentitiesUrls.TwoFactorService)]
public interface ITwoFactorService
{
	/// <summary>
	/// Returns whether a user has set up two-factor sign-in.
	/// </summary>
	/// <param name="dto">The id of the user.</param>
	Task<TwoFactorStatus> Status(IPrimaryKeyDto<long> dto);

	/// <summary>
	/// Starts the second step of a user's sign-in.
	/// </summary>
	/// <param name="dto">The user and the lifetime of the token issued at the end of the sign-in.</param>
	/// <returns>
	/// A <see cref="SignInStage.TwoFactor"/> challenge when the user has two-factor sign-in enabled, otherwise a
	/// <see cref="SignInStage.TwoFactorSetup"/> challenge.
	/// </returns>
	/// <remarks>
	/// The user's earlier challenges stop working, so a user has at most one open challenge.
	/// </remarks>
	Task<SignInResult> Challenge(IChallengeTwoFactorDto dto);

	/// <summary>
	/// Returns a new secret for a setup challenge.
	/// </summary>
	/// <param name="dto">The setup challenge.</param>
	/// <returns>The secret, or <see langword="null"/> when the challenge is unknown, expired or not a setup challenge.</returns>
	/// <remarks>
	/// Each call replaces the secret that is not confirmed yet, so only the latest secret's codes complete the challenge.
	/// </remarks>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<TwoFactorSetup?> Setup(IValueDto<string> dto);

	/// <summary>
	/// Completes a challenge with a code and issues the authentication token.
	/// </summary>
	/// <param name="dto">The challenge and the code.</param>
	/// <returns>
	/// The authentication token, or <see langword="null"/> when the challenge is unknown or expired or the code is
	/// wrong or already used.
	/// </returns>
	/// <remarks>
	/// A wrong code counts against the challenge, which stops working after
	/// <see cref="Configuration.Authentication.ITwoFactorConfiguration.MaxAttempts"/> wrong codes. Completing a setup
	/// challenge enables two-factor sign-in. When the code is checked against the user's secret, the operation's state
	/// carries the user, so a middleware can count the user's wrong codes; it is empty when the challenge is unknown or
	/// expired, the user is not active or has no secret to check.
	/// </remarks>
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<string?> Verify(IVerifyTwoFactorDto dto);

	/// <summary>
	/// Removes a user's secret and open challenges.
	/// </summary>
	/// <param name="dto">The id of the user.</param>
	/// <remarks>
	/// The user sets up two-factor sign-in again at the next sign-in when it is required, or when an application asks
	/// for a challenge. Authentication tokens issued earlier are not affected.
	/// </remarks>
	Task Reset(IPrimaryKeyDto<long> dto);
}
