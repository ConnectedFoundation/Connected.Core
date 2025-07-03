using Connected.Caching;
using Connected.Storage;

namespace Connected.Membership.Claims;

internal sealed class ClaimCache(ICachingService cache, IStorageProvider storage)
		: EntityCache<Claim, long>(cache, storage, MembershipMetaData.ClaimKey), IClaimCache
{
}
