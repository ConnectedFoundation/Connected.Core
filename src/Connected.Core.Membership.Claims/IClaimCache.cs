using Connected.Caching;

namespace Connected.Membership.Claims;

internal interface IClaimCache : IEntityCache<IClaim, long>
{
}
