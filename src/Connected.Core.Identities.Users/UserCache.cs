using Connected.Caching;
using Connected.Storage;

namespace Connected.Identities;

internal sealed class UserCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<IUser, User, long>(cache, storage, IdentitiesMetaData.UserKey), IUserCache
{
}
