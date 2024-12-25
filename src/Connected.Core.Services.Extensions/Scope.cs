using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public static class Scope
{
	public static AsyncServiceScope Create()
	{
		return ServiceExtensionsStartup.Services.CreateAsyncScope();
	}

	public static async Task<TDto> ResolveDto<TDto>()
		where TDto : IDto
	{
		using var scope = ServiceExtensionsStartup.Services.CreateAsyncScope();

		var result = scope.ServiceProvider.GetRequiredService<TDto>();

		await scope.Commit();

		return result;
	}

	public static TService? GetSingletonService<TService>()
	{
		return ServiceExtensionsStartup.Services.GetService<TService>();
	}

	public static HttpContext? HttpContext => GetSingletonService<IHttpContextAccessor>()?.HttpContext;
}
