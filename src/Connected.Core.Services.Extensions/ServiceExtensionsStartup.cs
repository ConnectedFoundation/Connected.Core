using Connected.Runtime;

namespace Connected.Services;
public sealed class ServiceExtensionsStartup : Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override async Task OnInitialize()
	{
		if (ServiceProvider is not null)
			Services = ServiceProvider;

		await base.OnInitialize();
	}
}
