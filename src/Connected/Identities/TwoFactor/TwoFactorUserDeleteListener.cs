using Connected.Annotations;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Identities.TwoFactor;
/// <summary>
/// Removes a deleted user's secret and challenges.
/// </summary>
[Middleware<IUserService>(ServiceEvents.Deleted)]
internal sealed class TwoFactorUserDeleteListener(ITwoFactorService twoFactor)
	: EventListener<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		await twoFactor.Reset(new PrimaryKeyDto<long> { Id = Dto.Id });
	}
}
