using Connected.Entities;
using Connected.Identities.Dtos;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Ops;

internal sealed class UpdatePassword(IUserService users, IUserCache cache, IStorageProvider storage, IEventService events)
	: ServiceAction<IUpdatePasswordDto>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await users.Select(Dto)) as User ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);
		var password = Dto.Password is null ? null : UserUtils.HashPassword(Dto.Password);

		await storage.Open<User>().Update(entity.Merge(Dto, State.Update, new
		{
			Password = password
		}), Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await users.Select(Dto)) as User ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);
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
