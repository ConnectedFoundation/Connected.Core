using Connected.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Reset(IIdentityAuthenticationTokenService identityAuthenticationTokenService)
	: ServiceFunction<IValueDto<string>, string?>
{
	protected override async Task<string?> OnInvoke()
	{
		var token = (await identityAuthenticationTokenService.Select(Dto)).Required();

		await identityAuthenticationTokenService.Delete(DtoFactory.Create<IPrimaryKeyDto<long>>(f =>
		{
			f.Id = token.Id;
		}));

		var key = Guid.NewGuid().ToString("N");

		var id = await identityAuthenticationTokenService.Insert(DtoFactory.Create<IInsertIdentityAuthenticationTokenDto>(f =>
		{
			f.Key = key;
			f.Token = token.Token;
			f.Identity = token.Identity;
			f.Status = AuthenticationTokenStatus.Enabled;
			f.Expire = DateTimeOffset.UtcNow.AddMinutes(20);
		}));

		return key;
	}
}
