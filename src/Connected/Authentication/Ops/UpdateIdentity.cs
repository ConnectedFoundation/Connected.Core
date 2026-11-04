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

		/*
		 * Clearing the identity has no token to raise the event with, and the event requires an id.
		 */
		if (Dto.Identity?.Token is { } token)
			await events.Updated(this, authentication, token);
	}
}
