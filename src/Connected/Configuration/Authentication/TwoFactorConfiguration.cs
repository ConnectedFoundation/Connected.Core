using Microsoft.Extensions.Configuration;

namespace Connected.Configuration.Authentication;

internal class TwoFactorConfiguration : ITwoFactorConfiguration
{
	public TwoFactorConfiguration(IConfiguration configuration)
	{
		configuration.Bind(this);

		/*
		 * A missing or invalid value falls back to its default here, so every consumer sees the same effective value.
		 */
		if (ChallengeLifetime <= 0)
			ChallengeLifetime = ITwoFactorConfiguration.DefaultChallengeLifetime;

		if (MaxAttempts <= 0)
			MaxAttempts = ITwoFactorConfiguration.DefaultMaxAttempts;

		if (LockoutDuration <= 0)
			LockoutDuration = ITwoFactorConfiguration.DefaultLockoutDuration;
	}

	public string? Key { get; set; }
	public bool Required { get; set; }
	public string? Issuer { get; set; }
	public int ChallengeLifetime { get; set; }
	public int MaxAttempts { get; set; }
	public int LockoutDuration { get; set; }
}
