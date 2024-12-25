using Connected.Runtime;

namespace Connected.Services;
internal sealed class Bootstrapper : Startup
{
	public static IServiceProvider Services { get; private set; } = default!;

	protected override async Task OnInitialize()
	{
		if (ServiceProvider is not null)
			Services = ServiceProvider;

		await base.OnInitialize();
	}
}
