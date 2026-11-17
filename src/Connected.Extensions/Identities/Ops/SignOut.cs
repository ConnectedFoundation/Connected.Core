using Connected.Authentication;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

/*
 * Dto carries the caller's own token, which Select(IValueDto<string>) resolves to exactly the
 * one row whose Token column matches it - not every row this identity has. Every token of the
 * identity is deleted, not only the one the caller happened to be holding, the same way
 * UserDeleteListener clears every token on account deletion.
 */
internal sealed class SignOut(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication)
	: ServiceAction<IValueDto<string>>
{
	protected override async Task OnInvoke()
	{
		await authentication.WithSystemIdentity();

		var caller = (await identityAuthenticationTokenService.Select(Dto)).Required();

		/*
		 * Every token of the identity is revoked, whatever its key: a session renewed by Reset is stored again
		 * under a key of its own and would otherwise outlive the sign-out.
		 */
		await identityAuthenticationTokenService.Revoke(caller.Identity);
	}
}
