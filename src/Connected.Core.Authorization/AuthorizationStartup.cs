using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authorization;
public sealed class AuthorizationStartup : Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddScoped<IAuthorizationContext, AuthorizationContext>();
	}
}
