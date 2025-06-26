using Connected.Services;

namespace Connected.Identities;

public static class UserExtensions
{
	public static async Task<bool> HasValidPassword(this IUserService users, long id)
	{
		var user = await users.Select(Dto.Factory.CreatePrimaryKey(id));

		if (user is null)
			return false;

		return user.Password is not null;
	}
}
