using Connected.Caching;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Membership.Roles;

internal sealed class RoleCache(ICachingService cache, IStorageProvider storage)
		: EntityCache<IRole, Role, int>(cache, storage, MembershipMetaData.RoleKey), IRoleCache
{
	protected override async Task<IImmutableList<IRole>?> OnInitializingEntities()
	{
		var stored = await base.OnInitializingEntities();

		var virtualRoles = new List<IRole>
		{
			new Role() { Id = -1, Name = VirtualRoles.Everyone, Status = Entities.Status.Enabled, Token = "C35E68D9-FF2F-4FC9-B532-A2F8DE974B64" },
			new Role() { Id = -2, Name = VirtualRoles.Authenticated, Status = Entities.Status.Enabled, Token = "1B75DA11-4E56-4146-8C64-B62FC805685D" },
			new Role() { Id = -3, Name = VirtualRoles.Anonimous, Status = Entities.Status.Enabled, Token = "24C27594-060F-4CD3-89DC-2E5CC3097B24" }
		};

		if (stored is not null)
			virtualRoles.AddRange(stored);

		return virtualRoles.ToImmutableList();
	}
}
