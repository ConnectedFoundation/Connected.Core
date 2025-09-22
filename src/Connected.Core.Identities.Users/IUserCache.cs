using Connected.Caching;

namespace Connected.Identities;

internal interface IUserCache : IEntityCache<IUser, long>
{
}
