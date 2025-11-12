using Connected.Services;

namespace Connected.Authentication.Ops;
internal sealed class UpdateIdentity(IAuthenticationService authentication) : ServiceAction<IUpdateIdentityDto>
{
	protected override async Task OnInvoke()
	{
		if (authentication is AuthenticationService service)
			service.Identity = Dto.Identity;

		await Task.CompletedTask;
	}
}
