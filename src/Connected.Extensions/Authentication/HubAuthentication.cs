using Connected.Net.Messaging;
using Connected.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Text;

namespace Connected.Authentication;

public static class HubAuthentication
{
	public static async Task Authenticate(this HubCallerContext context, IClients clients)
	{
		using var scope = await Scope.Create().WithSystemIdentity();
		var middlewares = scope.ServiceProvider.GetRequiredService<IMiddlewareService>();

		if (middlewares is null)
			return;

		var client = clients.Select(context.ConnectionId);
		var providers = await middlewares.Query<IAuthenticationProvider>();

		if (client?.Identity is not null)
			await AuthenticateWithClient(client.Identity!, providers);
		else
			await AuthenticateWithRequest(context, providers);
	}

	private static async Task AuthenticateWithClient(string identity, IImmutableList<IAuthenticationProvider> providers)
	{
		string? schema = "bearer";
		string? token = Convert.ToBase64String(Encoding.UTF8.GetBytes(identity));

		await Authenticate(schema, token, providers);
	}

	private static async Task AuthenticateWithRequest(HubCallerContext context, IImmutableList<IAuthenticationProvider> providers)
	{
		var header = context.GetHttpContext()?.Request.Headers.Authorization.ToString();

		string? schema = null;
		string? token = null;

		if (string.IsNullOrWhiteSpace(header))
		{
			header = context.GetHttpContext()?.Request.Query["access_token"].ToString();

			if (!string.IsNullOrWhiteSpace(header))
			{
				await Authenticate("bearer", header, providers);

				return;
			}
		}

		if (!string.IsNullOrWhiteSpace(header))
		{
			var tokens = header.Split(' ', 2);

			schema = tokens[0];

			if (tokens.Length > 1)
				token = tokens[1];
		}

		await Authenticate(schema, token, providers);
	}

	private static async Task Authenticate(string? schema, string? token, IImmutableList<IAuthenticationProvider> providers)
	{
		var dto = new AuthenticateDto
		{
			Schema = schema,
			Token = token
		};

		foreach (var provider in providers)
			await provider.Invoke(dto);
	}
}