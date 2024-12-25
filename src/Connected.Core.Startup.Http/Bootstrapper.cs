using Connected.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Startup.Http;

[Priority(55)]
internal sealed class Bootstrapper : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
		services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
		services.AddHttpClient();
	}
}