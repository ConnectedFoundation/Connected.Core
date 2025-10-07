using Connected.Identities.Users;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Users.Ops;

internal sealed class Delete(IUserService users, IUserCache cache, IStorageProvider storage, IEventService events)
  : ServiceAction<IPrimaryKeyDto<long>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await users.Select(Dto)) as User ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		await storage.Open<User>().Update(entity.Merge(Dto, Entities.State.Delete));
		await cache.Remove(Dto.Id);
		await events.Deleted(this, users, Dto.Id);
	}
}
