using Connected.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
/// <summary>
/// HTTP middleware that authenticates incoming requests by resolving registered
/// authentication providers and invoking them using credentials found in the
/// Authorization header or the "access_token" query parameter.
/// </summary>
/// <param name="next">The next request delegate in the pipeline.</param>
internal class RequestAuthentication(RequestDelegate next)
{
	/// <summary>
	/// Processes the current request by dispatching authentication and then calling the next delegate.
	/// </summary>
	/// <param name="context">The current HTTP context.</param>
	/// <returns>A task that completes when the operation finishes.</returns>
	public async Task InvokeAsync(HttpContext context)
	{
		/*
		 * Run the authentication step first to establish identity for the request
		 * and then continue down the middleware pipeline by invoking the next delegate.
		 */
		await OnInvoke(context);

		await next(context);
	}

	/// <summary>
	/// Performs the core authentication flow by querying providers and extracting credentials
	/// from headers or query parameters.
	/// </summary>
	/// <param name="context">The current HTTP context.</param>
	/// <returns>A task that completes when authentication dispatch has finished.</returns>
	private async Task OnInvoke(HttpContext context)
	{
		/*
		 * Create a system-elevated scope to access middleware services, then resolve the
		 * middleware registry to discover registered authentication providers.
		 */
		using var scope = await Scope.Create().WithSystemIdentity();
		var middlewares = scope.ServiceProvider.GetRequiredService<IMiddlewareService>();
		/*
		 * If the middleware registry could not be resolved, abort authentication.
		 */
		if (middlewares is null)
			return;
		/*
		 * Query the list of authentication providers that will process the request.
		 */
		var providers = await middlewares.Query<IAuthenticationProvider>();

		/*
		 * Attempt to read credentials from the Authorization header. If absent, fall
		 * back to the "access_token" query parameter often used for WebSocket scenarios.
		 */
		var header = context.Request.Headers.Authorization.ToString();

		string? schema = null;
		string? token = null;

		if (string.IsNullOrWhiteSpace(header))
			header = context?.Request.Query["access_token"].ToString();

		/*
		 * When a credential string is present, split into scheme and token parts using
		 * the first space as delimiter (e.g., "Bearer abc..." or "Basic xyz...").
		 */
		if (!string.IsNullOrWhiteSpace(header))
		{
			var tokens = header.Split(' ', 2);

			schema = tokens[0];

			if (tokens.Length > 1)
				token = tokens[1];
		}
		/*
		 * Build the DTO that carries the parsed scheme and token and invoke each provider
		 * sequentially to allow them to authenticate or update ambient identity state.
		 */
		var dto = new AuthenticateDto
		{
			Schema = schema,
			Token = token
		};

		foreach (var provider in providers)
			await provider.Invoke(dto);
	}
}