using Connected.Annotations.Entities;
using Connected.Entities;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Represents an in-memory entity for ad-hoc schema operation results.
/// </summary>
/// <remarks>
/// This entity is used to encapsulate the boolean result of ad-hoc schema operations
/// such as DDL statement execution, constraint modifications, or schema validation queries
/// against Oracle databases. The entity is marked with <see cref="PersistenceMode.InMemory"/>
/// to indicate it is not persisted to storage and exists only during runtime operations.
/// This lightweight record type provides a consistent entity-based interface for schema
/// operation results while avoiding unnecessary database persistence. Oracle-specific
/// operations include querying ALL_TABLES, ALL_TAB_COLUMNS, ALL_CONSTRAINTS, and ALL_INDEXES
/// system views for metadata discovery.
/// </remarks>
[Persistence(PersistenceMode.InMemory)]
internal sealed record AdHocSchemaEntity
	: Entity
{
	/// <summary>
	/// Gets the result of the ad-hoc schema operation.
	/// </summary>
	/// <value>
	/// <c>true</c> if the schema operation succeeded; otherwise, <c>false</c>.
	/// </value>
	public bool Result { get; init; }
}
