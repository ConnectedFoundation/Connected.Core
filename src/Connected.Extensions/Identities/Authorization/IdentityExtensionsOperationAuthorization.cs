using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Identities.Authorization;

/*
 * Ping, Reset, TokenInfo and SignOut each take the token they act on as a plain parameter instead
 * of acting on whoever is calling. Without this, anyone who merely obtains a token value - from a
 * log, a URL, anywhere - could keep it alive, replace it, read its expiry or end it, with no
 * session of their own. Requiring the parameter to match the caller's own current token keeps
 * these open before a full sign-in (that is their purpose - see IdentityExtensionsAuthorization)
 * while confining each caller to acting on their own token.
 */
internal abstract class OwnTokenAuthorization(IAuthenticationService authentication)
	: ServiceOperationAuthorization<IValueDto<string>>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		var identity = await authentication.SelectIdentity();

		return identity is not null && identity.Token == Dto.Value ? AuthorizationResult.Skip : AuthorizationResult.Fail;
	}
}

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.Ping))]
internal sealed class PingAuthorization(IAuthenticationService authentication) : OwnTokenAuthorization(authentication);

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.Reset))]
internal sealed class ResetAuthorization(IAuthenticationService authentication) : OwnTokenAuthorization(authentication);

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.TokenInfo))]
internal sealed class TokenInfoAuthorization(IAuthenticationService authentication) : OwnTokenAuthorization(authentication);

[Middleware<IdentityExtensions>(nameof(IIdentityExtensions.SignOut))]
internal sealed class SignOutAuthorization(IAuthenticationService authentication) : OwnTokenAuthorization(authentication);

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
