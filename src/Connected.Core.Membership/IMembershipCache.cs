using Connected.Caching;

namespace Connected.Membership;

internal interface IMembershipCache : IEntityCache<IMembership, long>
{
}
