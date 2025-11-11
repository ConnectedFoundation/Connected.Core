using Connected.Net.Events;
using Connected.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Connected;
/// <summary>
/// Application startup component that wires core runtime services and configures routing behavior.
/// </summary>
/// <remarks>
/// Registers eventing infrastructure and selects either the in-process routing server or a client proxy
/// based on configuration. Also initializes the maintenance identity token used for privileged operations.
/// </remarks>
internal sealed class Bootstrapper
	: Startup
{
	/// <summary>
	/// Gets the decoded maintenance identity token used for privileged maintenance operations.
	/// </summary>
	public static string? MaintenanceIdentityToken { get; private set; }
	/// <summary>
	/// Configures services required by the application at startup.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <inheritdoc cref="Startup.OnConfigureServices(IServiceCollection)"/>
	protected override void OnConfigureServices(IServiceCollection services)
	{
		/*
		 * Register eventing components as singletons so event messages, clients, and subscriptions can be coordinated
		 * across the process lifetime without per-request allocations.
		 */
		services.AddSingleton<EventMessages>();
		services.AddSingleton<EventClients>();
		services.AddSingleton<EventSubscriptions>();
		/*
		 * Determine routing mode from configuration. When no explicit routing server URL is provided, register the in-process
		 * routing server implementation; otherwise register the client proxy that communicates with an external routing server.
		 */
		var server = Configuration.GetValue<string?>("routing:routingServerUrl");

		if (server is null)
			RuntimeExtensions.AddService(typeof(Net.Routing.Server.RoutingService), services);
		else
			RuntimeExtensions.AddService(typeof(Net.Routing.Client.RoutingService), services);
	}
	/// <summary>
	/// Asynchronously initializes startup state such as the maintenance identity token.
	/// </summary>
	/// <returns>A task that completes when initialization is finished.</returns>
	/// <inheritdoc cref="Startup.OnInitialize()"/>
	protected override async Task OnInitialize()
	{
		/*
		 * Read the maintenance identity token from configuration (base64 encoded) and decode it for runtime use. This token
		 * enables maintenance-mode operations that must bypass standard identity flows during early startup.
		 */
		var token = Configuration.GetSection("identities:maintenance").Value;

		if (token is not null)
			MaintenanceIdentityToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));
		/*
		 * No additional asynchronous work is required at this point; return a completed task to satisfy the async contract.
		 */
		await Task.CompletedTask;
	}
}
