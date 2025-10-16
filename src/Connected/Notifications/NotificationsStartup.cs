using Connected.Net.Events;
using Connected.Net.Messaging;
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
		app.UseEndpoints((f) =>
		{
			f.MapHub<EventHub>(Hub);
		});
	}
}
