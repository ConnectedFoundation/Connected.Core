using Connected.Annotations.Entities;
using Connected.Data.AuditTrail;
using Connected.Data.AuditTrail.Dtos;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Data.Dtos;

[Table(Schema = SchemaAttribute.CoreSchema)]
internal sealed class InsertAuditTrailDto : EntityDto, IInsertAuditTrailDto
{
	[MaxLength(128)]
	public string? Property { get; set; }

	[MaxLength(1024)]
	public string? Value { get; set; }

	[MaxLength(1024)]
	public string? Description { get; set; }

	[MaxLength(128)]
	public string? Identity { get; set; }

	public AuditTrailVerb Verb { get; set; } = AuditTrailVerb.Update;
}
