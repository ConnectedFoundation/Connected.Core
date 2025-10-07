using Connected.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Connected;
internal sealed class Bootstrapper : Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		//TODO: resolve grpc routing and register either server or client
	}
}
