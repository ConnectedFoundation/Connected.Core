using Connected.Collections.Queues;
using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected;

internal sealed class Bootstrapper
	: Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddTransient(typeof(IDebounceContext<,,,>), typeof(DebounceContext<,,,>));
	}
}
