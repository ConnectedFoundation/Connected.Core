using Connected.Caching;

namespace Connected.Membership;

internal interface IMembershipCache : IEntityCache<Membership, long>
{
}
