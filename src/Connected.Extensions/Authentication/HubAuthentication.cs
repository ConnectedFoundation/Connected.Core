using Connected.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;

public static class HubAuthentication
{
	public static async Task Authenticate(this HubCallerContext context)
	{
		using var scope = await Scope.Create().WithSystemIdentity();
		var middlewares = scope.ServiceProvider.GetRequiredService<IMiddlewareService>();

		if (middlewares is null)
			return;

		var providers = await middlewares.Query<IAuthenticationProvider>();
		var header = context.GetHttpContext()?.Request.Headers.Authorization.ToString();
		string? schema = null;
		string? token = null;

		if (!string.IsNullOrWhiteSpace(header))
		{
			var tokens = header.Split(' ', 2);

			schema = tokens[0];

			if (tokens.Length > 1)
				token = tokens[1];
		}

		var dto = new AuthenticateDto
		{
			Schema = schema,
			Token = token
		};

		foreach (var provider in providers)
			await provider.Invoke(dto);
	}
}