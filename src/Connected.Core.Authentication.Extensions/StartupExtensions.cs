using Microsoft.AspNetCore.Builder;

namespace Connected.Authentication;
public static class StartupExtensions
{
	public static void ActivateRequestAuthentication(this IApplicationBuilder builder)
	{
		builder.UseMiddleware<RequestAuthentication>();
	}
}
