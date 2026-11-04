using Connected.Notifications;
using Connected.Services;

namespace Connected.Authentication.Ops;

internal sealed class UpdateIdentity(IAuthenticationService authentication, IEventService events)
	: ServiceAction<IUpdateIdentityDto>
{
	protected override async Task OnInvoke()
	{
		if (authentication is AuthenticationService service)
			service.Identity = Dto.Identity;

		await events.Updated(this, authentication, Dto.Identity?.Token);
	}
}
