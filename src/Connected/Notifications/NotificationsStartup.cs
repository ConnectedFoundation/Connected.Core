using Connected.Net.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Notifications;
public sealed class NotificationsStartup : Runtime.Startup
{
	internal const string Hub = "/hubs/notifications/events";

	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<EventServer>();
		services.AddHostedService<EventWorker>();
	}

	protected override void OnConfigure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		/*
		 * Without workers nothing drains or cleans the queued messages, so clients must not connect at all.
		 */
		if (!Application.WorkersEnabled)
			return;

		app.UseEndpoints((f) =>
		{
			f.MapHub<EventHub>(Hub);
		});
	}
}
