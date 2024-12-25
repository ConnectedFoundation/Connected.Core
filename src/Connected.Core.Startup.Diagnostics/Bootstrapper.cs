using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Connected.Startup.Diagnostics;

internal sealed class Bootstrapper : Runtime.Startup
{
	public static IServiceProvider Services { get; private set; } = default!;
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddLogging(builder => builder.AddConsole());
	}
}