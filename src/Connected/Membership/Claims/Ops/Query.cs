using Connected.Entities;
using Connected.Membership.Claims.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims.Ops;

internal class Query(IClaimCache cache)
  : ServiceFunction<IQueryClaimDto, IImmutableList<IClaim>>
{
	protected override async Task<IImmutableList<IClaim>> OnInvoke()
	{
		return await cache.AsEntities<IClaim>(f =>
					(Dto.Identity is null || string.Equals(f.Identity, Dto.Identity, StringComparison.Ordinal))
				&& (Dto.Schema is null || string.Equals(f.Schema, Dto.Schema, StringComparison.OrdinalIgnoreCase))
				&& (Dto.EntityId is null || string.Equals(f.Entity, Dto.Entity, StringComparison.OrdinalIgnoreCase))
				&& (Dto.EntityId is null || string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase)));
	}
}
