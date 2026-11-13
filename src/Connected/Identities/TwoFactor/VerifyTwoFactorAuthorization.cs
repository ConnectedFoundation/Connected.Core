using Connected.Annotations;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Identities.TwoFactor.Dtos;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Opens <see cref="ITwoFactorService.Verify"/> to anonymous callers; the challenge is the credential.
/// </summary>
[Middleware<ITwoFactorService>(nameof(ITwoFactorService.Verify))]
internal sealed class VerifyTwoFactorAuthorization
	: ServiceOperationAuthorization<IVerifyTwoFactorDto>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Pass);
	}
}
