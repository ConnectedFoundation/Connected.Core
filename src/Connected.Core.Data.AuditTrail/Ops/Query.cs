using Connected.Data.AuditTrail;
using Connected.Entities;
using Connected.Services;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Data.Ops;

internal sealed class Query(IStorageProvider storage)
	: ServiceFunction<IEntityDto, ImmutableList<IAuditTrail>>
{
	protected override async Task<ImmutableList<IAuditTrail>> OnInvoke()
	{
		return await storage.Open<AuditTrailEntry>().AsEntities<IAuditTrail>(f => string.Equals(f.Entity, Dto.Entity, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase));
	}
}
