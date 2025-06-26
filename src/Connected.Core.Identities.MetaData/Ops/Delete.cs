using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.MetaData.Ops;

internal sealed class Delete(IStorageProvider storage, IIdentityMetaDataCache cache, IEventService events, IIdentityMetaDataService metaData)
  : ServiceAction<IPrimaryKeyDto<string>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await metaData.Select(Dto)) as IdentityMetaData ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		await storage.Open<IdentityMetaData>().Update(entity.Merge(Dto, Entities.State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, metaData, Dto.Id);
	}
}
