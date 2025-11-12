using Connected.Entities;
using Connected.Membership.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Ops;

internal class Query(IMembershipCache cache)
  : ServiceFunction<IQueryMembershipDto, IImmutableList<IMembership>>
{
	protected override async Task<IImmutableList<IMembership>> OnInvoke()
	{
		return await cache.AsEntities<IMembership>(f =>
				(Dto.Identity is null || string.Equals(f.Identity, Dto.Identity, StringComparison.OrdinalIgnoreCase))
			&& (Dto.Role is null || f.Role == Dto.Role.GetValueOrDefault()));
	}
}
