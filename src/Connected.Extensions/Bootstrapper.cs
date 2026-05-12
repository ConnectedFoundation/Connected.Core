using Connected.Runtime;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected;
internal sealed class Bootstrapper : Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddTransient<IDto, Dto>();
	}
}
