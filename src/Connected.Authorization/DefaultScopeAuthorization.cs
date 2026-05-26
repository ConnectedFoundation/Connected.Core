using Connected.Annotations;
using Connected.Authentication;
using Connected.Authorization.Services;

namespace Connected.Authorization;

[ServiceRegistration(ServiceRegistrationMode.Manual)]
internal sealed class DefaultScopeAuthorization(IAuthenticationService authentication)
	: ScopeAuthorization
{
	internal AuthorizationResult? _result = null;
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		if (_result.HasValue)
			return _result.Value;

		if (await authentication.SelectIdentity() is null)
			_result = AuthorizationResult.Fail;

		_result = AuthorizationResult.Pass;

		return _result.Value;
	}
}
