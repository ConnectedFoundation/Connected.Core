using Connected.Identities.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Ops;

internal sealed class Update(IUserService users, IUserCache cache, IStorageProvider storage, IEventService events)
	: ServiceAction<IUpdateUserDto>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await users.Select(Dto)) as User ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		await storage.Open<User>().Update(entity.Merge(Dto, Entities.State.Update), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await users.Select(Dto)) as User ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);
		}, Caller);

		await cache.Refresh(Dto.Id);
		await events.Updated(this, users, Dto.Id);
	}
}
