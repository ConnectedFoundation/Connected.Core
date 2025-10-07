using Connected.Entities;
using Connected.Services;

namespace Connected.Membership.Roles.Ops;

internal class SelectByName(IRoleCache cache)
  : ServiceFunction<INameDto, IRole?>
{
	protected override async Task<IRole?> OnInvoke()
	{
		return await cache.AsEntity(f => string.Equals(f.Name, Dto.Name, StringComparison.OrdinalIgnoreCase));
	}
}
