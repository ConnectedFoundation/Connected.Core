using Connected.Entities;
using Connected.Services;

namespace Connected.Membership.Roles.Ops;

internal class Select(IRoleCache cache)
  : ServiceFunction<IPrimaryKeyDto<int>, IRole?>
{
	protected override async Task<IRole?> OnInvoke()
	{
		return await cache.AsEntity(f => f.Id == Dto.Id);
	}
}
