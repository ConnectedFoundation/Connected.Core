using Connected.Caching;

namespace Connected.Membership.Roles;

internal interface IRoleCache
	: IEntityCache<Role, int>
{
}
