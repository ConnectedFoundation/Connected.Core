namespace Connected.Configuration.Authentication;
/// <summary>
/// Two-factor configuration with no key that requires no code.
/// </summary>
/// <remarks>
/// Backs the default implementation of <see cref="IAuthenticationConfiguration.TwoFactor"/>.
/// </remarks>
internal sealed class DisabledTwoFactorConfiguration
	: ITwoFactorConfiguration
{
	/// <summary>
	/// Gets the shared instance.
	/// </summary>
	public static DisabledTwoFactorConfiguration Instance { get; } = new();

	/// <inheritdoc/>
	public string? Key => null;

	/// <inheritdoc/>
	public bool Required => false;

	/// <inheritdoc/>
	public string? Issuer => null;

	/// <inheritdoc/>
	public int ChallengeLifetime => ITwoFactorConfiguration.DefaultChallengeLifetime;

	/// <inheritdoc/>
	public int MaxAttempts => ITwoFactorConfiguration.DefaultMaxAttempts;
}
