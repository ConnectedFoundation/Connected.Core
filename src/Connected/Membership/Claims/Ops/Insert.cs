using Connected.Entities;
using Connected.Membership.Claims.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Claims.Ops;

internal class Insert(IStorageProvider storage, IClaimService claims, IEventService events, IClaimCache cache)
  : ServiceFunction<IInsertClaimDto, long>
{
	protected override async Task<long> OnInvoke()
	{
		var entity = await storage.Open<Claim>().Update(Dto.AsEntity<Claim>(State.Add)) ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		SetState(entity);

		await cache.Refresh(entity.Id);
		await events.Inserted(this, claims, entity.Id);

		return entity.Id;
	}
}
