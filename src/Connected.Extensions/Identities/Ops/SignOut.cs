using Connected.Authentication;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

/*
 * Dto carries the caller's own token, which Select(IValueDto<string>) resolves to exactly the
 * one row whose Token column matches it - not every row this identity has. Ensure usually keeps
 * just one AuthenticationToken-keyed row per identity and reuses it, but it does that with
 * FirstOrDefault, not a uniqueness guarantee, so this deletes every AuthenticationToken-keyed
 * row for the identity - not only the one the caller happened to be holding - the same way
 * UserDeleteListener clears every token on account deletion.
 */
internal sealed class SignOut(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication)
	: ServiceAction<IValueDto<string>>
{
	protected override async Task OnInvoke()
	{
		var caller = (await identityAuthenticationTokenService.Select(Dto)).Required();

		await authentication.WithSystemIdentity();

		var tokens = await identityAuthenticationTokenService.Query(DtoFactory.Create<IQueryIdentityAuthenticationTokensDto>(f =>
		{
			f.Identity = caller.Identity;
			f.Key = IdentityAuthenticationTokens.AuthenticationToken;
		}));

		foreach (var token in tokens)
		{
			await identityAuthenticationTokenService.Delete(DtoFactory.Create<IPrimaryKeyDto<long>>(f =>
			{
				f.Id = token.Id;
			}));
		}
	}
}
