using Connected.Annotations;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;

[Priority(60)]
public sealed class ServiceExtensionsStartup : Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	public static IServiceCollection ServicesCollection { get; private set; } = default!;
	protected override void OnConfigureServices(IServiceCollection services)
	{
		ServicesCollection = services;

		base.OnConfigureServices(services);
	}

	protected override async Task OnInitialize()
	{
		if (ServiceProvider is not null)
			Services = ServiceProvider;

		await base.OnInitialize();
	}
}
