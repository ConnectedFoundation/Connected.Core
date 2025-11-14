using Connected.Identities;
using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Authentication.Ops;
internal sealed class SelectIdentity(IAuthenticationService authentication, IHttpContextAccessor? http) : ServiceFunction<IDto, IIdentity?>
{
	protected override async Task<IIdentity?> OnInvoke()
	{
		if (authentication is not AuthenticationService service)
			return null;

		return await Task.FromResult(service.Identity);
	}
}
