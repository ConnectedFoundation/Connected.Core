using Connected.Caching;

namespace Connected.Identities.Users;

internal interface IUserCache : IEntityCache<IUser, long>
{
}
