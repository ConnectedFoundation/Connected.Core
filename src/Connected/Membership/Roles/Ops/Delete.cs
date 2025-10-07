using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Roles.Ops;

internal class Delete(IStorageProvider storage, IRoleService roles, IEventService events, IRoleCache cache)
  : ServiceAction<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await roles.Select(Dto)) as Role ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<Role>().Update(entity.Merge(Dto, Entities.State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, roles, Dto.Id);
	}
}
