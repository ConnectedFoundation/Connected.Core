using Connected.Entities;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Claims.Ops;

internal class Delete(IStorageProvider storage, IClaimService claims, IEventService events, IClaimCache cache)
  : ServiceAction<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await claims.Select(Dto)) as Claim ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<Claim>().Update(entity.Merge(Dto, State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, claims, Dto.Id);
	}
}
