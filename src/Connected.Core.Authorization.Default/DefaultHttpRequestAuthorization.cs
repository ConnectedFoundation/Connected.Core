using Connected.Authentication;
using Connected.Authorization;
using Connected.Authorization.Net;

namespace Connected.Core.Authorization.Default;

internal sealed class DefaultHttpRequestAuthorization(IAuthenticationService authentication) : HttpRequestAuthorization
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		if (await authentication.SelectIdentity() is null)
			return AuthorizationResult.Fail;

		return AuthorizationResult.Pass;
	}
}
