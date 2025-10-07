using Connected.Annotations;
using Connected.Authorization;
using Connected.Authorization.Services;

namespace Connected.Authentication.Authorization;

[Middleware<IAuthenticationService>()]
internal sealed class AuthenticationServiceAuthorization : ServiceAuthorization
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Pass);
	}
}
