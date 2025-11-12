using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Describes a database object (table) structure including columns, constraints, and indexes.
/// </summary>
/// <remarks>
/// This record represents the complete structure of a PostgreSQL database table as read from
/// the system catalogs. It contains collections of columns, constraints, and other metadata
/// needed for schema comparison and synchronization operations. The descriptor is populated
/// by querying PostgreSQL system tables and serves as the basis for determining necessary
/// schema modifications during synchronization.
/// </remarks>
internal sealed record ObjectDescriptor
{
	/// <summary>
	/// Gets or sets the collection of columns in the table.
	/// </summary>
	public List<ISchemaColumn> Columns { get; init; } = [];

	/// <summary>
	/// Gets or sets the collection of constraints on the table.
	/// </summary>
	public List<ObjectConstraint> Constraints { get; init; } = [];
}
