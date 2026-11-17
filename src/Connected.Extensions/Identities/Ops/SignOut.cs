using Connected.Authentication;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

/*
 * Dto carries the caller's own token, which Select(IValueDto<string>) resolves to one row only.
 * Signing out ends every sign-in session of the identity, not only the one the caller happened
 * to be holding: all AuthenticationToken-keyed rows are deleted, as before, and so is every copy
 * of the caller's own token - a session renewed by Reset is stored again under a key of its own
 * and would otherwise outlive the sign-out. Tokens of other kinds and other sessions' renewed
 * copies are left alone.
 */
internal sealed class SignOut(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication)
	: ServiceAction<IValueDto<string>>
{
	protected override async Task OnInvoke()
	{
		await authentication.WithSystemIdentity();

		var caller = (await identityAuthenticationTokenService.Select(Dto)).Required();

		var tokens = await identityAuthenticationTokenService.Query(DtoFactory.Create<IQueryIdentityAuthenticationTokensDto>(f =>
		{
			f.Identity = caller.Identity;
		}));

		foreach (var token in tokens)
		{
			if (!string.Equals(token.Key, IdentityAuthenticationTokens.AuthenticationToken, StringComparison.Ordinal)
				&& !string.Equals(token.Token, caller.Token, StringComparison.Ordinal))
				continue;

			await identityAuthenticationTokenService.Delete(DtoFactory.Create<IPrimaryKeyDto<long>>(f =>
			{
				f.Id = token.Id;
			}));
		}
	}
}
