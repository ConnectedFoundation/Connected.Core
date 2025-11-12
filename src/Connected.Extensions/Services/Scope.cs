using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public static class Scope
{
	public static AsyncServiceScope Create()
	{
		if (ServiceExtensionsStartup.Services is null)
			return ServiceExtensionsStartup.ServicesCollection.BuildServiceProvider(false).CreateAsyncScope();

		return ServiceExtensionsStartup.Services.CreateAsyncScope();
	}

	public static TService? GetSingletonService<TService>()
	{
		if (ServiceExtensionsStartup.Services is null)
		{
			var provider = ServiceExtensionsStartup.ServicesCollection.BuildServiceProvider(false);

			return provider.GetService<TService>();
		}

		return ServiceExtensionsStartup.Services.GetService<TService>();
	}

	public static HttpContext? HttpContext => GetSingletonService<IHttpContextAccessor>()?.HttpContext;
}
