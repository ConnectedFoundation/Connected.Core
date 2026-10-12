using Connected.Authentication;
using Connected.Identities.Authentication;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class TokenInfo(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication)
	: ServiceFunction<IValueDto<string>, IIdentityAuthenticationToken?>
{
	protected override async Task<IIdentityAuthenticationToken?> OnInvoke()
	{
		await authentication.WithSystemIdentity();

		return await identityAuthenticationTokenService.Select(Dto);
	}
}
