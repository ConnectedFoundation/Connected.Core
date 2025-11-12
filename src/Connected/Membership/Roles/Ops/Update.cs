using Connected.Membership.Roles.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Roles.Ops;

internal class Update(IStorageProvider storage, IRoleService roles, IEventService events, IRoleCache cache)
  : ServiceAction<IUpdateRoleDto>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await roles.Select(Dto)) as Role ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		await storage.Open<Role>().Update(entity.Merge(Dto, Entities.State.Update), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await roles.Select(Dto)) as Role ?? throw new NullReferenceException(Strings.ErrEntityExpected);
		}, Caller);

		await cache.Refresh(entity.Id);
		await events.Updated(this, roles, entity.Id);
	}
}
