using Connected.Annotations;
using Connected.Authorization;
using Connected.Authorization.Services;

namespace Connected.Identities.Authorization;

[Middleware<IdentityExtensions>]
internal sealed class IdentityExtensionsAuthorization
	: ServiceAuthorization
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Pass);
	}
}
