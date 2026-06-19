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
		var query = cache.AsQueryable();

		if (Dto.Identities is { Count: > 0 })
			query = query.Where(f => Dto.Identities.Contains(f.Identity, StringComparer.OrdinalIgnoreCase));

		if (Dto.Schemas is { Count: > 0 })
			query = query.Where(f => Dto.Schemas.Contains(f.Schema, StringComparer.OrdinalIgnoreCase));

		if (Dto.Entities is { Count: > 0 })
			query = query.Where(f => Dto.Entities.Contains(f.Entity, StringComparer.OrdinalIgnoreCase));

		if (Dto.EntityIds is { Count: > 0 })
			query = query.Where(f => Dto.EntityIds.Contains(f.EntityId, StringComparer.OrdinalIgnoreCase));

		if (Dto.Values is { Count: > 0 })
			query = query.Where(f => Dto.Values.Contains(f.Value, StringComparer.OrdinalIgnoreCase));
		
		if (Dto.Statuses is { Count: > 0 })
			query = query.Where(f => Dto.Statuses.Contains(f.Status));

		return await query.AsEntities();
	}
}
