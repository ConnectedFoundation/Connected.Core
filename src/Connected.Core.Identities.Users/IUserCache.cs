using Connected.Caching;

namespace Connected.Identities;

internal interface IUserCache : IEntityCache<User, long>
{
}
