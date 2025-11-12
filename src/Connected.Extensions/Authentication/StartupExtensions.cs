using Microsoft.AspNetCore.Builder;

namespace Connected.Authentication;

/// <summary>
/// Provides startup extension helpers for wiring authentication middleware into the
/// application's request processing pipeline.
/// </summary>
public static class StartupExtensions
{
	/// <summary>
	/// Registers the <see cref="RequestAuthentication"/> middleware to enable request-based
	/// authentication using registered <see cref="IAuthenticationProvider"/> implementations.
	/// </summary>
	/// <param name="builder">The application builder used to configure the middleware pipeline.</param>
	public static void ActivateRequestAuthentication(this IApplicationBuilder builder)
	{
		/*
		 * Insert the request authentication middleware into the pipeline so that each
		 * incoming HTTP request is processed for authentication before subsequent handlers.
		 */
		builder.UseMiddleware<RequestAuthentication>();
	}
}
