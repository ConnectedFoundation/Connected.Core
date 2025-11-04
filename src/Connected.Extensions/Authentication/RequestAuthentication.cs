using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;

internal class RequestAuthentication(RequestDelegate next)
{
	public async Task InvokeAsync(HttpContext context)
	{
		await OnInvoke(context);
		await next(context);
	}

	protected HttpContext? Context { get; private set; }

	private async Task OnInvoke(HttpContext context)
	{
		Context = context;

		using var scope = await Scope.Create().WithSystemIdentity();
		var middlewares = scope.ServiceProvider.GetRequiredService<IMiddlewareService>();

		if (middlewares is null)
			return;

		var providers = await middlewares.Query<IAuthenticationProvider>();
		var header = Context?.Request.Headers.Authorization.ToString();
		string? schema = null;
		string? token = null;

		if (string.IsNullOrWhiteSpace(header))
			header = Context?.Request.Query["access_token"].ToString();

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