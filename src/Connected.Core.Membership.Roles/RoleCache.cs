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
			new() { Id = -1, Name = VirtualRoles.Everyone, Status = Entities.Status.Enabled, Token = "C35E68D9-FF2F-4FC9-B532-A2F8DE974B64" },
			new() { Id = -2, Name = VirtualRoles.Authenticated, Status = Entities.Status.Enabled, Token = "1B75DA11-4E56-4146-8C64-B62FC805685D" },
			new() { Id = -3, Name = VirtualRoles.Anonimous, Status = Entities.Status.Enabled, Token = "24C27594-060F-4CD3-89DC-2E5CC3097B24" }
		};

		if (stored is not null)
			virtualRoles.AddRange(stored);

		return virtualRoles.ToImmutableList();
	}
}
