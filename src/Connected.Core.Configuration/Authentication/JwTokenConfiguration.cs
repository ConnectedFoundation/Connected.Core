using Microsoft.Extensions.Configuration;

namespace Connected.Configuration.Authentication;

internal class JwTokenConfiguration : IJwTokenConfiguration
{
	public JwTokenConfiguration(IConfiguration configuration)
	{
		configuration.Bind(this);
	}

	public string? Issuer { get; set; } = "Tom PIT";
	public string? Audience { get; set; } = "Tom PIT";
	public string? Key { get; set; } = "D78RF30487F4G0F8Z34F834F";
	public int Duration { get; set; } = 30;
}
