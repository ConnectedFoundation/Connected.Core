using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization.Services;

namespace Connected.Authorization;

[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class DefaultScopeAuthorization(IAuthenticationService authentication)
	: ScopeAuthorization
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		if (await authentication.SelectIdentity() is null)
			return AuthorizationResult.Fail;

		return AuthorizationResult.Pass;
	}
}
