using Connected.Identities.Dtos;
using Connected.Services;

namespace Connected.Identities.Ops;

internal sealed class SelectByCredentials(IUserService users)
  : ServiceFunction<ISelectUserDto, IUser?>
{
	protected override async Task<IUser?> OnInvoke()
	{
		var user = await users.Select(Dto.CreateValue(Dto.User));

		if (user is null)
			return null;

		if (user.Status != UserStatus.Active)
			return null;

		if (user.Password is null)
			return null;

		var hashed = await UserUtils.HashPassword(Dto.Password);

		if (!string.Equals(user.Password, hashed, StringComparison.Ordinal))
			return null;

		return user;
	}
}
