using Connected.Entities;
using Connected.Identities.Dtos;
using Connected.Identities.Users;
using Connected.Notifications;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Users.Ops;

internal sealed class Insert(IUserService users, IUserCache cache, IStorageProvider storage, IEventService events, IInsertUserAmbient ambient)
  : ServiceFunction<IInsertUserDto, long>
{
	protected override async Task<long> OnInvoke()
	{
		var password = Dto.Password is null ? null : await UserUtils.HashPassword(Dto.Password);

		var entity = await storage.Open<User>().Update(Dto.AsEntity<User>(State.Add, ambient, new
		{
			Password = password
		})) ?? throw new NullReferenceException(Connected.Strings.ErrEntityExpected);

		SetState(entity);

		await cache.Refresh(entity.Id);
		await events.Inserted(this, users, entity.Id);

		return entity.Id;
	}
}
