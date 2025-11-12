using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Ops;

internal class Delete(IStorageProvider storage, IMembershipService membership, IEventService events, IMembershipCache cache)
  : ServiceAction<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await membership.Select(Dto)) as Membership ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<Membership>().Update(entity.Merge(Dto, Entities.State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, membership, Dto.Id);
	}
}
