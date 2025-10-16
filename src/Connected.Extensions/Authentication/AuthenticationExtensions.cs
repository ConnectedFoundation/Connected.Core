using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
public static class AuthenticationExtensions
{
	public static async Task<AsyncServiceScope> WithSystemIdentity(this AsyncServiceScope scope)
	{
		var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		var dto = Dto.Factory.Create<IUpdateIdentityDto>();

		dto.Identity = new SystemIdentity();

		await service.UpdateIdentity(dto);

		return scope;
	}

	public static async Task WithSystemIdentity(this IAuthenticationService authentication)
	{
		var dto = Dto.Factory.Create<IUpdateIdentityDto>();

		dto.Identity = new SystemIdentity();

		await authentication.UpdateIdentity(dto);
	}
}
