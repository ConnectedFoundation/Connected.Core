using Connected.Annotations;

namespace Connected.Startup.Mvc;

[Priority(45)]
public sealed class MvcStartup : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddRazorPages();
	}

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (app is WebApplication web)
		{
			web.MapRazorPages()
				.WithStaticAssets();
		}
	}
}