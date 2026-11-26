using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Services;
using Microsoft.AspNetCore.Http;

namespace Connected.Identities.Authorization;

/*
 * Ping, Reset, TokenInfo and SignOut each take the token they act on as a plain parameter instead
 * of acting on whoever is calling. Without this, anyone who merely obtains a token value - from a
 * log, a URL, anywhere - could keep it alive, replace it, read its expiry or end it, with no
 * session of their own. Each caller may act only on the session it is signed in with: the token
 * passed must be the authentication token the request itself was authenticated with
 * (HttpIdentity.AuthenticationToken, set by TokenAuthentication). That token was found valid and
 * enabled for this request, so a lapsed or disabled token, another session of the same identity or
 * a row of another kind cannot be passed instead. A caller without such a token - anonymous, signed
 * in otherwise, or outside an HTTP request - is refused.
 */
internal abstract class OwnTokenAuthorization(IHttpContextAccessor http)
	: ServiceOperationAuthorization<IValueDto<string>>
{
	protected override Task<AuthorizationResult> OnInvoke()
	{
		var current = http.HttpContext?.User.Identity is HttpIdentity { IsAuthenticated: true } identity ? identity.AuthenticationToken : null;

		return Task.FromResult(!string.IsNullOrEmpty(current) && string.Equals(current, Dto.Value, StringComparison.Ordinal)
			? AuthorizationResult.Skip
			: AuthorizationResult.Fail);
	}
}

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.Ping))]
internal sealed class PingAuthorization(IHttpContextAccessor http) : OwnTokenAuthorization(http);

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.Reset))]
internal sealed class ResetAuthorization(IHttpContextAccessor http) : OwnTokenAuthorization(http);

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.TokenInfo))]
internal sealed class TokenInfoAuthorization(IHttpContextAccessor http) : OwnTokenAuthorization(http);

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.SignOut))]
internal sealed class SignOutAuthorization(IHttpContextAccessor http) : OwnTokenAuthorization(http);

// QueryClaims takes no token parameter (it already reads the ambient identity itself, see Ops/QueryClaims.cs,
// which today dereferences a possibly-null identity) - it only needs *some* authenticated caller.
[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.QueryClaims))]
internal sealed class QueryClaimsAuthorization(IAuthenticationService authentication)
	: ServiceOperationAuthorization<IDto>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		return await authentication.SelectIdentity() is null ? AuthorizationResult.Fail : AuthorizationResult.Skip;
	}
}
