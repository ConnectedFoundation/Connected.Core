namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Represents an index descriptor for Oracle index creation operations.
/// </summary>
/// <remarks>
/// This class encapsulates the information needed to create an index on an Oracle table.
/// It is used during schema synchronization to generate CREATE INDEX DDL statements.
/// Oracle indexes can be unique or non-unique and support various types including B-tree
/// (default), bitmap, function-based, and domain indexes. Index names are limited to
/// 30 characters (pre-12.2) or 128 characters (12.2+) and must be unique within a schema.
/// Composite indexes can include multiple columns with significance determined by column order.
/// </remarks>
internal sealed class IndexDescriptor
{
	/// <summary>
	/// Gets or sets the index name.
	/// </summary>
	/// <value>
	/// The unique index identifier to be created within the schema.
	/// </value>
	/// <remarks>
	/// Oracle index names must be unique within a schema and follow standard identifier rules.
	/// Names are case-insensitive and stored in uppercase unless quoted during creation.
	/// </remarks>
	public required string Name { get; set; }

	/// <summary>
	/// Gets or sets the schema name where the index will be created.
	/// </summary>
	/// <value>
	/// The Oracle schema (user) name, or <c>null</c> to use the current user's schema.
	/// </value>
	/// <remarks>
	/// In Oracle, schemas and users are synonymous. If not specified, the index is created
	/// in the current user's schema.
	/// </remarks>
	public string? Schema { get; set; }

	/// <summary>
	/// Gets or sets the table name on which the index will be created.
	/// </summary>
	/// <value>
	/// The name of the table to be indexed.
	/// </value>
	/// <remarks>
	/// The table must exist in the specified schema before the index can be created.
	/// Oracle stores table information in ALL_TABLES or USER_TABLES views.
	/// </remarks>
	public required string TableName { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the index enforces uniqueness.
	/// </summary>
	/// <value>
	/// <c>true</c> to create a unique index; <c>false</c> for a non-unique index.
	/// </value>
	/// <remarks>
	/// Unique indexes prevent duplicate values in the indexed columns. Oracle automatically
	/// creates unique indexes for primary key and unique constraints. Attempting to insert
	/// duplicate values in a unique indexed column results in ORA-00001 error.
	/// </remarks>
	public bool IsUnique { get; set; }

	/// <summary>
	/// Gets or sets the columns to include in the index.
	/// </summary>
	/// <value>
	/// An ordered list of column names that comprise the index key.
	/// </value>
	/// <remarks>
	/// The column order is significant for index efficiency. Oracle uses left-to-right
	/// matching for index seeks. For a composite index on (A, B, C), queries filtering on
	/// A, (A, B), or (A, B, C) can use the index efficiently, but queries filtering only
	/// on B or C cannot. Each column name should match the case used in the table definition
	/// or be properly quoted for case-sensitive identifiers.
	/// </remarks>
	public List<string> Columns { get; set; } = [];
}
