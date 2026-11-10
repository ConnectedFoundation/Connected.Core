using Connected.Authentication;
using Connected.Configuration;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Ping(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication, IConfigurationService configuration)
	: ServiceAction<IValueDto<string>>
{
	protected override async Task OnInvoke()
	{
		var token = (await identityAuthenticationTokenService.Select(Dto)).Required();

		if (!token.IsValid(configuration.Authentication.Tokens))
			throw new NullReferenceException(SR.ErrInvalidToken);

		// No expiry means "remember me" - pinging it must not give it one
		if (token.Expire is null)
			return;

		await authentication.WithSystemIdentity();

		await identityAuthenticationTokenService.Update(DtoFactory.Create<IUpdateIdentityAuthenticationTokenDto>(f =>
		{
			f.Expire = configuration.Authentication.Tokens.ResolveExpire(token.Created, IdentityAuthenticationTokenExtensions.DefaultRenewTimeout);
			f.Token = token.Token;
			f.Status = token.Status;
			f.Id = token.Id;
		}));
	}
}
