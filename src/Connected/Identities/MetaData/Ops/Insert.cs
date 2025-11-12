using Connected.Entities;
using Connected.Identities.MetaData.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.MetaData.Ops;

internal sealed class Insert(IStorageProvider storage, IIdentityMetaDataCache cache, IEventService events, IIdentityMetaDataService metaData)
  : ServiceAction<IInsertIdentityMetaDataDto>
{
	protected override async Task OnInvoke()
	{
		var entity = await storage.Open<IdentityMetaData>().Update(Dto.AsEntity<IdentityMetaData>(State.Add)) ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		SetState(entity);

		await cache.Refresh(Dto.Id);
		await events.Inserted(this, metaData, Dto.Id);
	}
}
