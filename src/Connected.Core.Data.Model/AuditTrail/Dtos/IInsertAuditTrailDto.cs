using Connected.Services;

namespace Connected.Data.AuditTrail.Dtos;

public interface IInsertAuditTrailDto : IEntityDto
{
	string? Property { get; set; }
	string? Value { get; set; }
	string? Description { get; set; }
	string? Identity { get; set; }
	AuditTrailVerb Verb { get; set; }
}
