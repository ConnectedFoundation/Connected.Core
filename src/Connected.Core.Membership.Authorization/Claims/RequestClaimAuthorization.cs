using Connected.Annotations;
using Connected.Authorization;
using Connected.Authorization.Services;
using Connected.Membership.Claims.Dtos;

namespace Connected.Membership.Claims;

[Middleware<IClaimService>(nameof(IClaimService.Request))]
internal sealed class RequestClaimAuthorization : ServiceOperationAuthorization<IRequestClaimDto>
{
	protected override async Task<AuthorizationResult> OnInvoke()
	{
		/*
		 * Allow access to everyone.
		 */
		return await Task.FromResult(AuthorizationResult.Pass);
	}
}
