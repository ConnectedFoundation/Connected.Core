using Connected.Authentication;
using Connected.Authorization.Net;

namespace Connected.Core.Authorization.Default;

internal sealed class DefaultHttpRequestAuthorization(IAuthenticationService authentication) : HttpRequestAuthorization
{
	protected override async Task OnInvoke()
	{
		if (await authentication.SelectIdentity() is null)
			throw new UnauthorizedAccessException();
	}
}
