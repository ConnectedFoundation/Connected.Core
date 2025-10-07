using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Roles.Ops;

internal class LookupByTokens(IRoleCache cache)
  : ServiceFunction<IValueListDto<string>, IImmutableList<IRole>>
{
	protected override async Task<IImmutableList<IRole>> OnInvoke()
	{
		return await cache.AsEntities(f => Dto.Items.Any(g => string.Equals(g, f.Token, StringComparison.OrdinalIgnoreCase)));
	}
}
