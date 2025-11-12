using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Represents a column definition from an existing Oracle database table.
/// </summary>
/// <remarks>
/// This class implements <see cref="ISchemaColumn"/> and encapsulates complete metadata about
/// a table column as retrieved from Oracle data dictionary views (ALL_TAB_COLUMNS). It includes
/// information about data types, nullability, default values, identity configuration, precision,
/// scale, and constraint participation. The class is used during schema synchronization to compare
/// existing database structure with desired schema definitions. Oracle-specific features include
/// handling of NUMBER types with precision/scale, VARCHAR2 vs CLOB distinction, and GENERATED AS
/// IDENTITY columns (12c+).
/// </remarks>
internal sealed class ExistingColumn
	: ISchemaColumn
{
	/// <inheritdoc/>
	public required string Name { get; init; }

	/// <inheritdoc/>
	public DbType DataType { get; init; }

	/// <inheritdoc/>
	public bool IsNullable { get; init; }

	/// <inheritdoc/>
	public int MaxLength { get; init; }

	/// <inheritdoc/>
	public int Precision { get; init; }

	/// <inheritdoc/>
	public int Scale { get; init; }

	/// <inheritdoc/>
	public int DatePrecision { get; init; }

	/// <inheritdoc/>
	public DateKind DateKind { get; init; }

	/// <inheritdoc/>
	public BinaryKind BinaryKind { get; init; }

	/// <inheritdoc/>
	public bool IsIdentity { get; init; }

	/// <inheritdoc/>
	public bool IsVersion { get; init; }

	/// <inheritdoc/>
	public string? DefaultValue { get; init; }

	/// <inheritdoc/>
	public bool IsPrimaryKey { get; set; }

	/// <inheritdoc/>
	public bool IsIndex { get; set; }

	/// <inheritdoc/>
	public string? Index { get; set; }

	/// <inheritdoc/>
	public bool IsUnique { get; set; }

	/// <inheritdoc/>
	public PropertyInfo? Property { get; init; }

	/// <inheritdoc/>
	public bool Equals(ISchemaColumn? other)
	{
		if (other is null)
			return false;

		/*
		 * Compare column identity by name (case-insensitive for Oracle)
		 */
		return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
	}
}
