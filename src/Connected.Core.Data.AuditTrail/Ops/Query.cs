using Connected.Data.AuditTrail;
using Connected.Entities;
using Connected.Services;
using Connected.Storage;
using System.Collections.Immutable;

namespace Connected.Data.Ops;

internal sealed class Query(IStorageProvider storage)
	: ServiceFunction<IEntityDto, IImmutableList<IAuditTrail>>
{
	protected override async Task<IImmutableList<IAuditTrail>> OnInvoke()
	{
		return await storage.Open<AuditTrailEntry>().AsEntities<IAuditTrail>(f => string.Equals(f.Entity, Dto.Entity, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase));
	}
}
