using Microsoft.Extensions.Configuration;

namespace Connected.Configuration.Authentication;

internal class TwoFactorConfiguration : ITwoFactorConfiguration
{
	public TwoFactorConfiguration(IConfiguration configuration)
	{
		configuration.Bind(this);
	}

	public string? Key { get; set; }
	public bool Required { get; set; }
	public string? Issuer { get; set; }
	public int ChallengeLifetime { get; set; } = ITwoFactorConfiguration.DefaultChallengeLifetime;
	public int MaxAttempts { get; set; } = ITwoFactorConfiguration.DefaultMaxAttempts;
}
