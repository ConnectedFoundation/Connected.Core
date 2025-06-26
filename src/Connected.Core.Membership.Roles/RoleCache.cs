using Connected.Caching;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Membership.Roles;

internal sealed class RoleCache(ICachingService cache, IStorageProvider storage)
		: EntityCache<Role, int>(cache, storage, MembershipMetaData.RoleKey), IRoleCache
{
	protected override async Task<IImmutableList<Role>?> OnInitializingEntities()
	{
		var stored = await base.OnInitializingEntities();

		var virtualRoles = new List<Role>
		{
			new() { Id = -1, Name = VirtualRoles.Everyone, Status = Entities.Status.Enabled },
			new() { Id = -2, Name = VirtualRoles.Authenticated, Status = Entities.Status.Enabled },
			new() { Id = -3, Name = VirtualRoles.Anonimous, Status = Entities.Status.Enabled }
		};

		if (stored is not null)
			virtualRoles.AddRange(stored);

		return virtualRoles.ToImmutableList();
	}
}
