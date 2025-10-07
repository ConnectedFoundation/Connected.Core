using Microsoft.Extensions.Configuration;

namespace Connected.Configuration.Authentication;

internal class AuthenticationConfiguration(IConfiguration configuration) : IAuthenticationConfiguration
{
	public IJwTokenConfiguration JwToken { get; } = new JwTokenConfiguration(configuration);
}
