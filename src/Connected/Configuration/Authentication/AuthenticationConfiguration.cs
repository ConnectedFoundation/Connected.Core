using Microsoft.Extensions.Configuration;

namespace Connected.Configuration.Authentication;

internal class AuthenticationConfiguration(IConfiguration configuration) : IAuthenticationConfiguration
{
	public IJwTokenConfiguration JwToken { get; } = new JwTokenConfiguration(configuration);
	public IAuthenticationTokenConfiguration Tokens { get; } = new AuthenticationTokenConfiguration(configuration.GetSection("tokens"));
	public ITwoFactorConfiguration TwoFactor { get; } = new TwoFactorConfiguration(configuration.GetSection("twoFactor"));
}
