using Connected.Entities;
using Connected.Services;

namespace Connected.Membership.Roles.Ops;

internal class SelectByToken(IRoleCache cache)
  : ServiceFunction<IValueDto<string>, IRole?>
{
	protected override async Task<IRole?> OnInvoke()
	{
		return await cache.AsEntity(f => string.Equals(f.Token, Dto.Value, StringComparison.OrdinalIgnoreCase));
	}
}
