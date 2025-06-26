using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class Resolve(IUserCache cache)
  : ServiceFunction<IValueDto<string>, IUser?>
{
	protected override async Task<IUser> OnInvoke()
	{
		if (await SelectByEmail() is IUser emailUser)
			return emailUser;

		if (await SelectByToken() is IUser tokenUser)
			return tokenUser;

		if (await SelectById() is IUser idUser)
			return idUser;

		return null;
	}

	private async Task<IUser?> SelectByEmail()
	{
		return await cache.Get(f => string.Equals(f.Email, Dto.Value, StringComparison.OrdinalIgnoreCase));
	}

	private async Task<IUser?> SelectByToken()
	{
		return await cache.Get(f => string.Equals(f.Token, Dto.Value, StringComparison.Ordinal));
	}

	private async Task<IUser?> SelectById()
	{
		if (!long.TryParse(Dto.Value, out long id))
			return null;

		return await cache.Get(id);
	}

}
