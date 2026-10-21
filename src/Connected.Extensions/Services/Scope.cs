using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public static class Scope
{
	public static AsyncServiceScope Create()
	{
		if (ServiceExtensionsStartup.Services is null)
			return ServiceExtensionsStartup.BootstrapProvider().CreateAsyncScope();

		return ServiceExtensionsStartup.Services.CreateAsyncScope();
	}

	public static TService? GetSingletonService<TService>()
	{
		if (ServiceExtensionsStartup.Services is null)
			return ServiceExtensionsStartup.BootstrapProvider().GetService<TService>();

		return ServiceExtensionsStartup.Services.GetService<TService>();
	}

	public static HttpContext? HttpContext => GetSingletonService<IHttpContextAccessor>()?.HttpContext;
}
