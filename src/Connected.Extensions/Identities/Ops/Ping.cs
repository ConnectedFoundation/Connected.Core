using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Ping(IIdentityAuthenticationTokenService identityAuthenticationTokenService)
	: ServiceAction<IValueDto<string>>
{
	protected override async Task OnInvoke()
	{
		var token = (await identityAuthenticationTokenService.Select(Dto)).Required();

		if (token.Status != AuthenticationTokenStatus.Enabled || (token.Expire.HasValue && token.Expire.Value < DateTimeOffset.UtcNow))
			throw new NullReferenceException(SR.ErrInvalidToken);

		await identityAuthenticationTokenService.Update(DtoFactory.Create<IUpdateIdentityAuthenticationTokenDto>(f =>
		{
			f.Expire = DateTimeOffset.UtcNow.AddMinutes(20);
			f.Id = token.Id;
		}));
	}
}
