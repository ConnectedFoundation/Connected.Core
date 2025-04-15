using Connected.Data.AuditTrail.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Data.AuditTrail;

public interface IAuditTrailService
{
	Task<long> Insert(IInsertAuditTrailDto dto);
	Task<IImmutableList<IAuditTrail>> Query(IEntityDto dto);
	Task Delete(IEntityDto dto);
}
