using Microsoft.Extensions.DependencyInjection;

namespace Connected.Caching;

public sealed class CachingStartup : Runtime.Startup
{
	protected override void OnConfigureServices(IServiceCollection services)
	{
		services.AddScoped(typeof(ICacheContext), typeof(CacheContext));
	}
}