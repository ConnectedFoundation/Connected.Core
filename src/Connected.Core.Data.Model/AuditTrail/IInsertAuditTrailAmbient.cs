using Connected.Data.AuditTrail.Dtos;
using Connected.Services;

namespace Connected.Data.AuditTrail;

public interface IInsertAuditTrailAmbient : IAmbientProvider<IInsertAuditTrailDto>
{
	DateTimeOffset Created { get; set; }
}
