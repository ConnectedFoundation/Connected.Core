using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Collections.Immutable;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Represents a column from an existing database schema.
/// </summary>
/// <remarks>
/// This class encapsulates metadata about a column that already exists in the database,
/// as opposed to a column defined in entity metadata. It implements both <see cref="ISchemaColumn"/>
/// for standard column information and <see cref="IExistingSchemaColumn"/> for querying
/// index participation. The class is populated by querying database system tables or views
/// during schema discovery operations. It provides comprehensive column metadata including
/// data types, constraints, indexes, and precision settings needed for schema comparison
/// and synchronization operations.
/// </remarks>
internal class ExistingColumn(ISchema schema)
	: ISchemaColumn, IExistingSchemaColumn
{
	/// <inheritdoc/>
	public required string Name { get; set; }

	/// <inheritdoc/>
	public DbType DataType { get; set; }

	/// <inheritdoc/>
	public bool IsIdentity { get; set; }

	/// <inheritdoc/>
	public bool IsVersion { get; set; }

	/// <inheritdoc/>
	public bool IsUnique { get; set; }

	/// <inheritdoc/>
	public bool IsIndex { get; set; }

	/// <inheritdoc/>
	public bool IsPrimaryKey { get; set; }

	/// <inheritdoc/>
	public string? DefaultValue { get; set; }

	/// <inheritdoc/>
	public int MaxLength { get; set; }

	/// <inheritdoc/>
	public bool IsNullable { get; set; }

	/// <summary>
	/// Gets or sets the dependency type for the column.
	/// </summary>
	/// <value>
	/// The type name that this column depends on, or <c>null</c> if no dependency exists.
	/// </value>
	public string? DependencyType { get; set; }

	/// <summary>
	/// Gets or sets the dependency property for the column.
	/// </summary>
	/// <value>
	/// The property name that this column depends on, or <c>null</c> if no dependency exists.
	/// </value>
	public string? DependencyProperty { get; set; }

	/// <inheritdoc/>
	public string? Index { get; set; }

	/// <inheritdoc/>
	public int Precision { get; set; }

	/// <inheritdoc/>
	public int Scale { get; set; }

	/// <inheritdoc/>
	public DateKind DateKind { get; set; } = DateKind.DateTime;

	/// <inheritdoc/>
	public BinaryKind BinaryKind { get; set; } = BinaryKind.VarBinary;

	/// <inheritdoc/>
	public PropertyInfo? Property { get; set; }

	/// <inheritdoc/>
	public int DatePrecision { get; set; }

	/// <inheritdoc/>
	public ImmutableArray<string> QueryIndexColumns(string column)
	{
		if (schema is not ExistingSchema existing)
			return [];

		/*
		 * Search through all indexes in the existing schema to find indexes
		 * that contain the specified column.
		 */
		foreach (var index in existing.Indexes)
		{
			if (index.Columns.Contains(column, StringComparer.OrdinalIgnoreCase))
				return [.. index.Columns];
		}

		return [];
	}
}
