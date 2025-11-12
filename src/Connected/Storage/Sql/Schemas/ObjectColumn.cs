namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents column metadata retrieved from SQL Server system catalogs.
/// </summary>
/// <remarks>
/// This class encapsulates column information obtained from the sp_help stored procedure or
/// similar system catalog queries. It provides metadata about physical column properties including
/// data type, length, precision, nullability, and collation. This information is used during
/// schema discovery and comparison operations to build accurate representations of existing
/// database structures. The class maps directly to SQL Server's column metadata structure,
/// preserving both type information and physical storage characteristics.
/// </remarks>
internal class ObjectColumn
{
	/// <summary>
	/// Gets or sets the column name.
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	/// Gets or sets the data type name.
	/// </summary>
	/// <value>
	/// The SQL Server data type name (e.g., "int", "varchar", "datetime2").
	/// </value>
	public required string Type { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this is a computed column.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column value is computed from an expression; otherwise, <c>false</c>.
	/// </value>
	public bool Computed { get; set; }

	/// <summary>
	/// Gets or sets the maximum length for character or binary columns.
	/// </summary>
	/// <value>
	/// The maximum length in bytes or characters.
	/// </value>
	public int Length { get; set; }

	/// <summary>
	/// Gets or sets the numeric precision.
	/// </summary>
	/// <value>
	/// The total number of significant digits for numeric types.
	/// </value>
	public int Precision { get; set; }

	/// <summary>
	/// Gets or sets the numeric scale.
	/// </summary>
	/// <value>
	/// The number of digits to the right of the decimal point for numeric types.
	/// </value>
	public int Scale { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the column allows null values.
	/// </summary>
	public bool Nullable { get; set; }

	/// <summary>
	/// Gets or sets the trim trailing blanks setting for character columns.
	/// </summary>
	/// <value>
	/// A string indicating whether trailing blanks are trimmed, or <c>null</c> if not applicable.
	/// </value>
	public string? TrimTrailingBlanks { get; set; }

	/// <summary>
	/// Gets or sets the fixed length setting for character columns.
	/// </summary>
	/// <value>
	/// A string indicating whether the column is fixed-length, or <c>null</c> if not applicable.
	/// </value>
	public string? FixedLenInSource { get; set; }

	/// <summary>
	/// Gets or sets the collation name for character columns.
	/// </summary>
	/// <value>
	/// The collation name defining sort order and character comparison rules, or <c>null</c> if not applicable.
	/// </value>
	public string? Collation { get; set; }
}
