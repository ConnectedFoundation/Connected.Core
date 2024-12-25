using Microsoft.Extensions.DependencyInjection;

namespace Connected.Services;
public static class Scope
{
	public static AsyncServiceScope Create()
	{
		return Bootstrapper.Services.CreateAsyncScope();
	}

	public static async Task<TDto> ResolveDto<TDto>()
		where TDto : IDto
	{
		using var scope = Bootstrapper.Services.CreateAsyncScope();

		var result = scope.ServiceProvider.GetRequiredService<TDto>();

		await scope.Commit();

		return result;
	}
}
