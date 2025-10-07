using Connected.Entities;
using Connected.Identities.Dtos;
using Connected.Identities.Users;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Users.Ops;

internal sealed class UpdatePassword(IUserService users, IUserCache cache, IStorageProvider storage, IEventService events)
	: ServiceAction<IUpdatePasswordDto>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await users.Select(Dto) as User).Required();
		var password = Dto.Password is null ? null : await UserUtils.HashPassword(Dto.Password);

		await storage.Open<User>().Update(entity.Merge(Dto, State.Update, new
		{
			Password = password
		}), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await users.Select(Dto) as User).Required();
		}, Caller, async (f) =>
		{
			return await Task.FromResult(f.Merge(Dto, State.Update, new
			{
				Password = password
			}));
		});

		await cache.Refresh(Dto.Id);
		await events.Updated(this, users, Dto.Id);
	}
}
