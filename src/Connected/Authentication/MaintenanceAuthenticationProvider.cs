using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Connected.Authentication;
internal sealed class MaintenanceAuthenticationProvider(
	IConfiguration configuration,
	IHttpContextAccessor http,
	IAuthenticationService authentication)
	: BearerAuthenticationProvider
{
	protected override async Task OnAuthenticate()
	{
		var current = http.HttpContext?.User.Identity;

		if (current is not null && current.IsAuthenticated)
			return;

		var section = configuration.GetSection("identities");
		var token = section.GetValue<string>("maintenance");

		if (token is null)
			return;

		var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));

		if (!string.Equals(Token, raw, StringComparison.Ordinal))
			return;

		var dto = Dto.Create<IUpdateIdentityDto>();
		var identity = new MaintenanceIdentity
		{
			Token = raw
		};

		dto.Identity = identity;

		if (http.HttpContext is not null)
			http.HttpContext.User = new DefaultPrincipal(new HttpIdentity(identity));

		await authentication.UpdateIdentity(dto);
	}
}
