using Connected.Annotations.Entities;
using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Represents a column definition loaded from an existing PostgreSQL table.
/// </summary>
/// <remarks>
/// This class implements <see cref="ISchemaColumn"/> to provide compatibility with the schema
/// comparison framework. It encapsulates all column metadata retrieved from PostgreSQL system
/// catalogs including name, data type, constraints, and default values. The class is used during
/// schema synchronization to compare existing column definitions with desired schema definitions
/// and determine necessary modifications such as adding, altering, or dropping columns.
/// </remarks>
internal sealed class ExistingColumn
	: ISchemaColumn
{
	/// <inheritdoc/>
	public string Name { get; init; } = default!;

	/// <inheritdoc/>
	public DbType DataType { get; init; }

	/// <inheritdoc/>
	public int MaxLength { get; init; }

	/// <inheritdoc/>
	public bool IsNullable { get; init; }

	/// <inheritdoc/>
	public bool IsPrimaryKey { get; set; }

	/// <inheritdoc/>
	public bool IsIdentity { get; init; }

	/// <inheritdoc/>
	public bool IsUnique { get; init; }

	/// <inheritdoc/>
	public string? DefaultValue { get; init; }

	/// <inheritdoc/>
	public List<string>? IndexNames { get; init; }

	/// <inheritdoc/>
	public bool IsIndex { get; init; }

	/// <inheritdoc/>
	public bool IsVersion { get; init; }

	/// <inheritdoc/>
	public string? Index { get; init; }

	/// <inheritdoc/>
	public int Scale { get; init; }

	/// <inheritdoc/>
	public int Precision { get; init; }

	/// <inheritdoc/>
	public DateKind DateKind { get; init; }

	/// <inheritdoc/>
	public BinaryKind BinaryKind { get; init; }

	/// <inheritdoc/>
	public int DatePrecision { get; init; }

	/// <inheritdoc/>
	public System.Reflection.PropertyInfo? Property { get; init; }
}
