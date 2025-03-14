using Connected.Data.AuditTrail;
using Connected.Data.AuditTrail.Dtos;
using Connected.Services;

namespace Connected.Data;

internal sealed class InsertAuditTrailAmbient : AmbientProvider<IInsertAuditTrailDto>, IInsertAuditTrailAmbient
{
	public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
