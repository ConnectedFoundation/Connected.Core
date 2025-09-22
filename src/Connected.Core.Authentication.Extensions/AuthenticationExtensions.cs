using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Authentication;
public static class AuthenticationExtensions
{
	public static AsyncServiceScope WithSystemIdentity(this AsyncServiceScope scope)
	{
		var service = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
		var dto = Dto.Factory.Create<IUpdateIdentityDto>();

		dto.Identity = new SystemIdentity();

		service.UpdateIdentity(dto);

		return scope;
	}
}
