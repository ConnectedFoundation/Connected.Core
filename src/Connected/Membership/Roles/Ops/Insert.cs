using Connected.Entities;
using Connected.Membership.Roles.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Membership.Roles.Ops;

internal class Insert(IStorageProvider storage, IRoleService roles, IEventService events, IRoleCache cache, IInsertRoleAmbient ambient)
  : ServiceFunction<IInsertRoleDto, int>
{
	protected override async Task<int> OnInvoke()
	{
		var entity = await storage.Open<Role>().Update(Dto.AsEntity<Role>(State.Add, ambient)) ?? throw new NullReferenceException(Strings.ErrEntityExpected);

		SetState(entity);

		await cache.Refresh(entity.Id);
		await events.Inserted(this, roles, entity.Id);

		return entity.Id;
	}
}
