using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Roles.Ops;

internal class Query(IRoleCache cache)
  : ServiceFunction<IQueryDto, IImmutableList<IRole>>
{
	protected override async Task<IImmutableList<IRole>> OnInvoke()
	{
		return await cache.WithDto(Dto).AsEntities<IRole>();
	}
}
