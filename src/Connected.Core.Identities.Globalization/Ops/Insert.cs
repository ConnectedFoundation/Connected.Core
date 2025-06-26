using Connected.Entities;
using Connected.Identities.Globalization.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Globalization.Ops;

internal sealed class Insert(IIdentityGlobalizationCache cache, IStorageProvider storage, IEventService events, IIdentityGlobalizationService globalization)
	: ServiceAction<IInsertIdentityGlobalizationDto>
{
	protected override async Task OnInvoke()
	{
		await storage.Open<IdentityGlobalization>().Update(Dto.AsEntity<IdentityGlobalization>(State.Add));
		await cache.Refresh(Dto.Id);
		await events.Inserted(this, globalization, Dto.Id);
	}
}