using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Identities.Authentication;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Connected.Identities.Authorization;

/*
 * Ping, Reset, TokenInfo and SignOut each take the token they act on as a plain parameter instead
 * of acting on whoever is calling. Without this, anyone who merely obtains a token value - from a
 * log, a URL, anywhere - could keep it alive, replace it, read its expiry or end it, with no
 * session of their own. Requiring the token to belong to the caller keeps these open before a full
 * sign-in (that is their purpose - see IdentityExtensionsAuthorization) while confining each caller
 * to their own tokens.
 *
 * The parameter is an authentication token, while the caller's identity carries the identity's own
 * token, so the two are never equal: the token is looked up and its identity is compared instead.
 */
internal abstract class OwnTokenAuthorization(IAuthenticationService authentication)
	: ServiceOperationAuthorization<IValueDto<string>>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		if (string.IsNullOrEmpty(Dto.Value) || await authentication.SelectIdentity() is not IIdentity identity)
			return AuthorizationResult.Fail;

		/*
		 * Reading the token needs the system identity, which is taken in a scope of its own so the
		 * caller's identity is not changed.
		 */
		await using var scope = await Scope.Create().WithSystemIdentity();

		var token = await scope.ServiceProvider.GetRequiredService<IIdentityAuthenticationTokenService>().Select(Dto);

		return token is not null && string.Equals(token.Identity, identity.Token, StringComparison.Ordinal)
			? AuthorizationResult.Skip
			: AuthorizationResult.Fail;
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
