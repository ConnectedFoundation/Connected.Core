using Connected.Authentication;
using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Ping(IIdentityAuthenticationTokenService identityAuthenticationTokenService, IAuthenticationService authentication)
	: ServiceAction<IValueDto<string>>
{
	protected override async Task OnInvoke()
	{
		var token = (await identityAuthenticationTokenService.Select(Dto)).Required();

		if (!token.IsValid())
			throw new NullReferenceException(SR.ErrInvalidToken);

		await authentication.WithSystemIdentity();

		await identityAuthenticationTokenService.Update(DtoFactory.Create<IUpdateIdentityAuthenticationTokenDto>(f =>
		{
			f.Expire = DateTimeOffset.UtcNow.AddMinutes(20);
			f.Token = token.Token;
			f.Status = token.Status;
			f.Id = token.Id;
		}));
	}
}
