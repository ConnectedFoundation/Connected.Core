using Connected.Entities;
using Connected.Identities.Globalization.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Globalization.Ops;

internal sealed class Update(IIdentityGlobalizationCache cache, IStorageProvider storage, IEventService events, IIdentityGlobalizationService globalization)
	: ServiceAction<IUpdateIdentityGlobalizationDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await globalization.Select(Dto.CreatePrimaryKey(Dto.Id))) is not IdentityGlobalization existing)
			return;

		await storage.Open<IdentityGlobalization>().Update(Dto.AsEntity<IdentityGlobalization>(State.Update), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await globalization.Select(Dto.CreatePrimaryKey(Dto.Id))) as IdentityGlobalization;
		}, Caller);

		await cache.Refresh(Dto.Id);
		await events.Updated(this, globalization, Dto.Id);
	}
}