using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Authentication;
internal sealed class MaintenanceAuthenticationProvider(
	IHttpContextAccessor http,
	IAuthenticationService authentication)
	: BearerAuthenticationProvider
{
	protected override async Task OnAuthenticate()
	{
		var current = http.HttpContext?.User.Identity;

		if (current is not null && current.IsAuthenticated)
			return;

		if (Bootstrapper.MaintenanceIdentityToken is null)
			return;

		var raw = Bootstrapper.MaintenanceIdentityToken;

		if (!string.Equals(Token, raw, StringComparison.Ordinal))
			return;

		var dto = Dto.Create<IUpdateIdentityDto>();
		var identity = new MaintenanceIdentity
		{
			Token = raw
		};

		dto.Identity = identity;

		if (http.HttpContext is not null)
		{
			http.HttpContext.User = new DefaultPrincipal(new HttpIdentity(identity)
			{
				IsAuthenticated = true
			});
		}

		await authentication.UpdateIdentity(dto);
	}
}
