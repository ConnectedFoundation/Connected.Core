using Connected.Entities;
using Connected.Membership.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Ops;

internal class Insert(IStorageProvider storage, IMembershipService membership, IEventService events, IMembershipCache cache)
  : ServiceFunction<IInsertMembershipDto, long>
{
	protected override async Task<long> OnInvoke()
	{
		var entity = await storage.Open<Membership>().Update(Dto.AsEntity<Membership>(State.Add)) ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		SetState(entity);

		await cache.Refresh(entity.Id);
		await events.Inserted(this, membership, entity.Id);

		return entity.Id;
	}
}
