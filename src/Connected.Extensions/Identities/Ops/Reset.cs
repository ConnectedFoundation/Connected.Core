using Connected.Authentication;
using Connected.Configuration;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Reset(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication, IConfigurationService configuration)
	: ServiceFunction<IValueDto<string>, string?>
{
	protected override async Task<string?> OnInvoke()
	{
		await authentication.WithSystemIdentity();

		var token = (await identityAuthenticationTokenService.Select(Dto)).Required();

		/*
		 * The authorization admits only the token the request was authenticated with, which is valid; a token that is
		 * not is never renewed.
		 */
		if (!token.IsValid())
			throw new NullReferenceException(SR.ErrInvalidToken);

		await identityAuthenticationTokenService.Delete(DtoFactory.Create<IPrimaryKeyDto<long>>(f =>
		{
			f.Id = token.Id;
		}));

		var key = Guid.NewGuid().ToString("N");
		/*
		 * A reset turns a permanent token into an expiring one. Its original creation time would end the new
		 * token immediately under a maximum lifetime, so such a token starts a new lifetime instead.
		 */
		var created = token.Expire is null ? null : token.Created;

		var id = await identityAuthenticationTokenService.Insert(DtoFactory.Create<IInsertIdentityAuthenticationTokenDto>(f =>
		{
			f.Key = key;
			f.Token = token.Token;
			f.Identity = token.Identity;
			f.Status = AuthenticationTokenStatus.Enabled;
			f.Created = created;
			f.Expire = configuration.Authentication.Tokens.ResolveExpire(created ?? DateTimeOffset.UtcNow, IdentityAuthenticationTokenExtensions.DefaultRenewTimeout);
		}));

		return key;
	}
}
