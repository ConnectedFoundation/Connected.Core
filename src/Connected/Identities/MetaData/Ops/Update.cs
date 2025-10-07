using Connected.Identities.MetaData.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.MetaData.Ops;

internal sealed class Update(IStorageProvider storage, IIdentityMetaDataCache cache, IEventService events, IIdentityMetaDataService metaData)
  : ServiceAction<IUpdateIdentityMetaDataDto>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await metaData.Select(Dto)) as IdentityMetaData ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		await storage.Open<IdentityMetaData>().Update(Dto.AsEntity<IdentityMetaData>(Entities.State.Update), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await metaData.Select(Dto)) as IdentityMetaData ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);
		}, Caller);

		await cache.Refresh(Dto.Id);
		await events.Updated(this, metaData, Dto.Id);
	}
}
