using Connected.Authentication.Ops;
using Connected.Identities;
using Connected.Services;

namespace Connected.Authentication;

internal sealed class AuthenticationService(IServiceProvider services) : Service(services), IAuthenticationService
{
	internal IIdentity? Identity { get; set; }

	public async Task<IIdentity?> SelectIdentity()
	{
		return await Invoke(GetOperation<SelectIdentity>(), Dto.Empty);
	}

	public async Task UpdateIdentity(IUpdateIdentityDto dto)
	{
		await Invoke(GetOperation<UpdateIdentity>(), dto);
	}
}