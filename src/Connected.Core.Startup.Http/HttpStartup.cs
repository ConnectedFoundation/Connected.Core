using Connected.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Startup.Http;

[Priority(55)]
public sealed class HttpStartup : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
		services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
		services.AddHttpClient();
		services.AddResponseCompression();
		services.AddResponseCaching();
	}

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		app.UseResponseCompression();
		app.UseResponseCaching();
	}
}