using Connected.Annotations;
using Connected.Authentication;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Authorization;

[Middleware<IAuthenticationService>(ServiceEvents.Updated)]
internal sealed class UpdateIdentityListener(DefaultScopeAuthorization authorization)
	: EventListener<IPrimaryKeyDto<string?>>
{
	protected override async Task OnInvoke()
	{
		authorization._result = null;

		await Task.CompletedTask;
	}
}
