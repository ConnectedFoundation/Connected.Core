using Connected.Annotations;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Services;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Opens <see cref="ITwoFactorService.Setup"/> to anonymous callers; the setup challenge is the credential.
/// </summary>
[Middleware<ITwoFactorService>(nameof(ITwoFactorService.Setup))]
internal sealed class SetupTwoFactorAuthorization
	: ServiceOperationAuthorization<IValueDto<string>>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Pass);
	}
}
