using Connected.Storage.Schemas;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Describes an index definition for PostgreSQL database tables.
/// </summary>
/// <remarks>
/// This class encapsulates all information needed to create or manage an index including
/// the index name, associated columns, uniqueness constraint, and schema/table location.
/// It supports both single-column and composite indexes by maintaining a list of columns
/// that participate in the index. The descriptor is used during schema synchronization
/// to generate appropriate CREATE INDEX DDL statements for PostgreSQL.
/// </remarks>
internal sealed class IndexDescriptor
{
	/// <summary>
	/// Gets or sets the name of the index.
	/// </summary>
	public string Name { get; init; } = default!;

	/// <summary>
	/// Gets or sets the schema name where the index resides.
	/// </summary>
	public string? Schema { get; init; }

	/// <summary>
	/// Gets or sets the table name that the index belongs to.
	/// </summary>
	public string Table { get; init; } = default!;

	/// <summary>
	/// Gets or sets the columns that are included in the index.
	/// </summary>
	/// <remarks>
	/// For composite indexes, this list contains multiple columns in the order
	/// they should appear in the index definition.
	/// </remarks>
	public List<ISchemaColumn> Columns { get; init; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether the index enforces uniqueness.
	/// </summary>
	public bool IsUnique { get; init; }
}
