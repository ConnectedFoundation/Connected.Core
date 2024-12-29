using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Connected.Startup.Authentication;

public sealed class AuthenticationStartup : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		app.UseMiddleware<AuthenticationCookieMiddleware>();
	}
}