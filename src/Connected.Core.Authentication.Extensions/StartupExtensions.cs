using Connected.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
public static class StartupExtensions
{
	public static void AddBasicAuthentication(this IServiceCollection services)
	{
		services.AddMiddleware<BasicAuthentication>();
	}

	public static void ActivateAuthenticationCookieMiddleware(this IApplicationBuilder builder)
	{
		builder.UseMiddleware<AuthenticationCookieMiddleware>();
	}

	public static void ActivateRequestAuthentication(this IApplicationBuilder builder)
	{
		builder.UseMiddleware<RequestAuthentication>();
	}
}
