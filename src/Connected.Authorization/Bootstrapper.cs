using Connected;
using Connected.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authorization.Default;
internal sealed class Bootstrapper : Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		var section = Configuration.GetSection("authorization:default");
		var enabled = section.GetValue("enabled", true);

		if (enabled)
		{
			services.AddMiddleware<DefaultHttpRequestAuthorization>();
			services.AddMiddleware<DefaultScopeAuthorization>();
		}
	}
}
