using Connected.Net.Messaging;
using Connected.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Text;

namespace Connected.Authentication;

/// <summary>
/// Provides SignalR hub authentication helpers that resolve registered authentication
/// providers and invoke them using either a previously associated client identity
/// or credentials extracted from the inbound connection request.
/// </summary>
public static class HubAuthentication
{
	/// <summary>
	/// Authenticates the caller represented by the <paramref name="context"/> using
	/// either a stored client identity or an inbound authorization header / query token.
	/// </summary>
	/// <param name="context">The SignalR hub caller context.</param>
	/// <param name="clients">The client registry used to look up a stored client.</param>
	/// <returns>A task representing completion of authentication dispatch.</returns>
	public static async Task Authenticate(this HubCallerContext context, IClients clients)
	{
		/*
		 * Create a system-elevated scope to access middleware services required for
		 * authentication (e.g. registered providers). Using system identity ensures
		 * sufficient privileges for querying provider registrations.
		 */
		using var scope = await Scope.Create().WithSystemIdentity();
		var middlewares = scope.ServiceProvider.GetRequiredService<IMiddlewareService>();

		/*
		 * Acquire the hub client representation via its connection id to determine
		 * if we have a previously established identity that can be reused.
		 */
		var client = clients.Select(context.ConnectionId);

		/*
		 * Query all authentication providers registered as middleware so each provider
		 * can inspect and potentially authenticate the request.
		 */
		var providers = await middlewares.Query<IAuthenticationProvider>();

		/*
		 * If the client already has an identity, authenticate using that value as a
		 * bearer token; otherwise parse identity information from the inbound request.
		 */
		if (client?.Identity is not null)
			await AuthenticateWithClient(client.Identity!, providers);
		else
			await AuthenticateWithRequest(context, providers);
	}

	/// <summary>
	/// Authenticates using a previously stored client identity string by converting
	/// it into a bearer token and invoking all providers.
	/// </summary>
	/// <param name="identity">The stored client identity.</param>
	/// <param name="providers">Authentication providers to invoke.</param>
	/// <returns>A task completing after providers have been invoked.</returns>
	private static async Task AuthenticateWithClient(string identity, IImmutableList<IAuthenticationProvider> providers)
	{
		/*
		 * Convert the raw identity string into a Base64 encoded bearer token so providers
		 * expecting a bearer scheme can process it consistently.
		 */
		string? schema = "bearer";
		string? token = Convert.ToBase64String(Encoding.UTF8.GetBytes(identity));

		await Authenticate(schema, token, providers);
	}

	/// <summary>
	/// Authenticates using credentials extracted from the caller's HTTP context headers
	/// or query parameters when a stored client identity is not present.
	/// </summary>
	/// <param name="context">The hub caller context used to access HTTP request data.</param>
	/// <param name="providers">Authentication providers to invoke.</param>
	/// <returns>A task completing after providers have been invoked.</returns>
	private static async Task AuthenticateWithRequest(HubCallerContext context, IImmutableList<IAuthenticationProvider> providers)
	{
		/*
		 * Attempt to read the Authorization header first. If absent, fall back to the
		 * "access_token" query parameter (commonly used for WebSocket negotiations).
		 */
		var header = context.GetHttpContext()?.Request.Headers.Authorization.ToString();

		string? schema = null;
		string? token = null;

		/*
		 * If no Authorization header is present, check for access_token query parameter and
		 * directly authenticate using bearer semantics.
		 */
		if (string.IsNullOrWhiteSpace(header))
		{
			header = context.GetHttpContext()?.Request.Query["access_token"].ToString();

			if (!string.IsNullOrWhiteSpace(header))
			{
				await Authenticate("bearer", header, providers);

				return;
			}
		}

		/*
		 * When an Authorization header exists, split it into scheme and token components
		 * using the first space as a delimiter.
		 */
		if (!string.IsNullOrWhiteSpace(header))
		{
			var tokens = header.Split(' ', 2);

			schema = tokens[0];

			if (tokens.Length > 1)
				token = tokens[1];
		}

		await Authenticate(schema, token, providers);
	}

	/// <summary>
	/// Invokes each provider with a constructed authentication DTO containing the provided
	/// schema and token values.
	/// </summary>
	/// <param name="schema">Authentication scheme (e.g. bearer, basic).</param>
	/// <param name="token">Authentication token value.</param>
	/// <param name="providers">Provider list to iterate.</param>
	/// <returns>A task completing after all providers processed the DTO.</returns>
	private static async Task Authenticate(string? schema, string? token, IImmutableList<IAuthenticationProvider> providers)
	{
		/*
		 * Construct a DTO carrying the scheme and token so providers can apply their
		 * specific authentication logic. Iterate through providers sequentially to allow
		 * each to inspect or modify identity state.
		 */
		var dto = new AuthenticateDto
		{
			Schema = schema,
			Token = token
		};

		/*
		 * Sequentially invoke providers; ordering can be influenced by priority attributes on
		 * provider implementations.
		 */
		foreach (var provider in providers)
			await provider.Invoke(dto);
	}
}