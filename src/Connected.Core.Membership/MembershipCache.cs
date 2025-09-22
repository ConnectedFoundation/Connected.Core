using Connected.Caching;
using Connected.Storage;

namespace Connected.Membership;

internal sealed class MembershipCache(ICachingService cache, IStorageProvider storage)
	: EntityCache<IMembership, Membership, long>(cache, storage, MembershipMetaData.MembershipKey), IMembershipCache
{
}
