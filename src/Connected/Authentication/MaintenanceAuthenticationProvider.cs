using Microsoft.AspNetCore.Http;

namespace Connected.Authentication;
internal sealed class MaintenanceAuthenticationProvider(IHttpContextAccessor http)
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

		var identity = new MaintenanceIdentity
		{
			Token = raw
		};

		if (http.HttpContext is not null)
		{
			http.HttpContext.User = new DefaultPrincipal(new HttpIdentity(identity)
			{
				IsAuthenticated = true
			});
		}

		await Task.CompletedTask;
	}
}
