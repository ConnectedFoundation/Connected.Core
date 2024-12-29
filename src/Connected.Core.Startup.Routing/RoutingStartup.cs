using Connected.Annotations;
using Connected.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Startup.Routing;

[Priority(52)]
public sealed class RoutingStartup : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		app.UseRouting();

		using var scope = app.ApplicationServices.CreateAsyncScope();

		try
		{
			var rt = scope.ServiceProvider.GetRequiredService<IRuntimeService>();

			app.UseEndpoints(async routes =>
			{
				foreach (var startup in rt.QueryStartups().Result)
					await startup.ConfigureEndpoints(routes);
			});
		}
		catch
		{
			scope.Rollback().Wait();

			throw;
		}
		finally
		{
			scope.Flush().Wait();
		}
	}
}