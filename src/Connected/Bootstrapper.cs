using Connected.Net.Events;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Connected;
internal sealed class Bootstrapper : Startup
{
	public static string? MaintenanceIdentityToken { get; private set; }
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<EventMessages>();
		services.AddSingleton<EventClients>();
		services.AddSingleton<EventSubscriptions>();
		//TODO: resolve grpc routing and register either server or client
	}

	protected override async Task OnInitialize()
	{
		var token = Configuration.GetSection("identities:maintenance").Value;

		if (token is not null)
			MaintenanceIdentityToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));

		await Task.CompletedTask;
	}
}
