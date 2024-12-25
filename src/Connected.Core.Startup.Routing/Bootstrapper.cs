using Connected.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Connected.Startup.Routing;

[Priority(52)]
internal sealed class Bootstrapper : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		app.UseRouting();

		app.UseEndpoints(async routes =>
		{
			foreach (var startup in MicroServices.Startups)
				await startup.ConfigureEndpoints(routes);
		});
	}
}