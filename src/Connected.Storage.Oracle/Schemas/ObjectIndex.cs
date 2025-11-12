namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Represents an index defined on an Oracle database table.
/// </summary>
/// <remarks>
/// This class encapsulates metadata about database indexes including B-tree, bitmap,
/// function-based, and domain indexes. Oracle stores index information in ALL_INDEXES
/// and ALL_IND_COLUMNS system views. Index names are limited to 30 characters (pre-12.2)
/// or 128 characters (12.2+) and must be unique within a schema. Oracle automatically creates
/// indexes for primary key and unique constraints, and these system-generated indexes typically
/// follow specific naming patterns.
/// </remarks>
internal sealed class ObjectIndex
{
	/// <summary>
	/// Gets or sets the index name.
	/// </summary>
	/// <value>
	/// The unique index identifier within the schema.
	/// </value>
	/// <remarks>
	/// Oracle index names are case-insensitive and stored in uppercase unless quoted during
	/// creation. System-generated index names for constraints typically match the constraint
	/// name or follow the pattern SYS_Cnnnnn.
	/// </remarks>
	public required string Name { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the index enforces uniqueness.
	/// </summary>
	/// <value>
	/// <c>true</c> if the index is unique; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Unique indexes prevent duplicate values in the indexed columns. Oracle automatically
	/// creates unique indexes for primary key and unique constraints. The UNIQUENESS column
	/// in ALL_INDEXES indicates whether an index is UNIQUE or NONUNIQUE.
	/// </remarks>
	public bool IsUnique { get; set; }

	/// <summary>
	/// Gets or sets the columns included in the index.
	/// </summary>
	/// <value>
	/// An ordered list of column names that comprise the index key.
	/// </value>
	/// <remarks>
	/// The column order is significant for index efficiency and is preserved as defined in
	/// ALL_IND_COLUMNS.COLUMN_POSITION. Composite indexes can include multiple columns, with
	/// the leftmost columns being the most significant for index seek operations. For function-based
	/// indexes, column expressions may be stored rather than simple column names.
	/// </remarks>
	public List<string> Columns { get; set; } = [];

	/// <summary>
	/// Gets or sets the index type.
	/// </summary>
	/// <value>
	/// The Oracle index type such as NORMAL (B-tree), BITMAP, FUNCTION-BASED NORMAL, or DOMAIN.
	/// </value>
	/// <remarks>
	/// Oracle supports various index types retrieved from ALL_INDEXES.INDEX_TYPE:
	/// - NORMAL: Standard B-tree index (most common)
	/// - BITMAP: Bitmap index for low-cardinality columns
	/// - FUNCTION-BASED NORMAL: Index on computed expressions
	/// - DOMAIN: Specialized index for specific data types (e.g., spatial, text)
	/// </remarks>
	public string? IndexType { get; set; }
}
