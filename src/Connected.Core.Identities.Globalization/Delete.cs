using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Globalization;

internal sealed class Delete(IIdentityGlobalizationCache cache, IStorageProvider storage, IEventService events, IIdentityGlobalizationService globalization)
	: ServiceAction<IPrimaryKeyDto<string>>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await globalization.Select(new PrimaryKeyDto<string> { Id = Dto.Id })) is not IdentityGlobalization existing)
			return;

		await storage.Open<IdentityGlobalization>().Update(Dto.AsEntity<IdentityGlobalization>(State.Deleted));
		await cache.Remove(existing.Id);
		await events.Deleted(this, globalization, Dto.Id);
	}
}