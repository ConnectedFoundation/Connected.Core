using Microsoft.Extensions.Configuration;

namespace Connected.Configuration.Authentication;

internal class AuthenticationTokenConfiguration : IAuthenticationTokenConfiguration
{
	public AuthenticationTokenConfiguration(IConfiguration configuration)
	{
		configuration.Bind(this);
	}

	public int? IdleTimeout { get; set; }
	public int? MaxLifetime { get; set; }
}
