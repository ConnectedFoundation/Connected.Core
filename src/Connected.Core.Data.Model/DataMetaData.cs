using Connected.Annotations.Entities;
using Connected.Data.AuditTrail;

namespace Connected.Data;

public static class DataMetaData
{
	public const string AuditTrailKey = $"{SchemaAttribute.CoreSchema}.{nameof(IAuditTrail)}";
}
