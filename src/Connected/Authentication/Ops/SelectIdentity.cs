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

		if (service.Identity is not null)
			return service.Identity;

		if (http?.HttpContext is null)
			return null;

		if (http.HttpContext.User.Identity is HttpIdentity identity)
			return identity.Identity;

		await Task.CompletedTask;

		return null;
	}
}
