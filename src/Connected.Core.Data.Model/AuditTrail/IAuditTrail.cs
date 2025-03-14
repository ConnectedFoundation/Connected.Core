using Connected.Entities;

namespace Connected.Data.AuditTrail;

public enum AuditTrailVerb
{
	Create = 1,
	Update = 2,
	Delete = 3
}

public interface IAuditTrail : IEntityContainer<long>
{
	DateTimeOffset Created { get; init; }
	string? Identity { get; init; }
	string? Property { get; init; }
	string? Value { get; init; }
	string? Description { get; init; }
	AuditTrailVerb Verb { get; init; }
}
