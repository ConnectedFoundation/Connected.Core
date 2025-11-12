using Connected.Annotations.Entities;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a column definition in a database schema.
/// </summary>
/// <remarks>
/// This class encapsulates all metadata for a database column including its name, data type,
/// constraints, indexing properties, and relationship to entity properties. It implements both
/// <see cref="ISchemaColumn"/> for column metadata access and <see cref="IEquatable{T}"/> for
/// comprehensive column comparison operations. The equality implementation performs deep comparison
/// of all column characteristics including data types, constraints, precision settings, and index
/// configurations. This is essential for schema synchronization operations where changes must be
/// detected accurately to generate appropriate DDL statements. The class supports primary constructor
/// pattern for required dependencies like the parent schema and associated property.
/// </remarks>
internal class SchemaColumn(ISchema schema, PropertyInfo property)
	: IEquatable<ISchemaColumn>, ISchemaColumn
{
	/// <inheritdoc/>
	public required string Name { get; set; }

	/// <inheritdoc/>
	public DbType DataType { get; set; }

	/// <inheritdoc/>
	public bool IsIdentity { get; set; }

	/// <inheritdoc/>
	public bool IsUnique { get; set; }

	/// <inheritdoc/>
	public bool IsVersion { get; set; }

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
	public int DatePrecision { get; set; }

	/// <summary>
	/// Gets or sets the ordinal position of the column within the table.
	/// </summary>
	/// <value>
	/// An integer representing the column's position in the table structure.
	/// </value>
	/// <remarks>
	/// The ordinal determines the physical order of columns in the table definition,
	/// which can affect storage layout and query performance in some database systems.
	/// </remarks>
	public int Ordinal { get; set; }

	/// <inheritdoc/>
	public PropertyInfo Property { get; set; } = property;

	/// <summary>
	/// Determines whether the current column is equal to another column.
	/// </summary>
	/// <param name="other">The column to compare with the current column.</param>
	/// <returns>
	/// <c>true</c> if the specified column is equal to the current column; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	/// This method performs a comprehensive comparison of all column properties including name,
	/// data type, constraints, precision settings, and index configurations. For indexed columns,
	/// it also compares the index composition by verifying that all columns participating in the
	/// index match between the two schemas. This deep comparison ensures accurate change detection
	/// during schema synchronization operations.
	/// </remarks>
	public bool Equals(ISchemaColumn? other)
	{
		if (other is null)
			return false;

		/*
		 * Compare column names using case-insensitive comparison as database systems
		 * typically treat column names case-insensitively.
		 */
		if (!string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase))
			return false;

		/*
		 * Verify that both columns use the same database data type.
		 */
		if (DataType != other.DataType)
			return false;

		/*
		 * Compare identity (auto-increment) property.
		 */
		if (IsIdentity != other.IsIdentity)
			return false;

		/*
		 * Compare uniqueness constraint setting.
		 */
		if (IsUnique != other.IsUnique)
			return false;

		/*
		 * Compare index participation setting.
		 */
		if (IsIndex != other.IsIndex)
			return false;

		/*
		 * Compare version/rowversion column setting for optimistic concurrency.
		 */
		if (IsVersion != other.IsVersion)
			return false;

		/*
		 * Compare primary key participation.
		 */
		if (IsPrimaryKey != other.IsPrimaryKey)
			return false;

		/*
		 * Compare numeric precision for decimal and numeric types.
		 */
		if (Precision != other.Precision)
			return false;

		/*
		 * Compare numeric scale for decimal and numeric types.
		 */
		if (Scale != other.Scale)
			return false;

		/*
		 * Compare default value expressions using ordinal comparison.
		 */
		if (!string.Equals(DefaultValue, other.DefaultValue, StringComparison.Ordinal))
			return false;

		/*
		 * Compare maximum length for string and binary columns.
		 */
		if (MaxLength != other.MaxLength)
			return false;

		/*
		 * Compare nullability setting.
		 */
		if (IsNullable != other.IsNullable)
			return false;

		/*
		 * Compare date/time kind for temporal columns.
		 */
		if (DateKind != other.DateKind)
			return false;

		/*
		 * Compare date/time precision for fractional seconds.
		 */
		if (DatePrecision != other.DatePrecision)
			return false;

		/*
		 * Compare binary storage kind for binary columns.
		 */
		if (BinaryKind != other.BinaryKind)
			return false;

		/*
		 * For existing columns with index information, perform deep comparison
		 * of index composition to detect changes in multi-column indexes.
		 */
		if (other is IExistingSchemaColumn existing && Name is not null)
		{
			var existingColumns = existing.QueryIndexColumns(Name);

			if (existingColumns.Any() || IsIndex)
			{
				var columns = new List<string>();

				/*
				 * Collect all columns that participate in the same named index.
				 */
				if (!string.IsNullOrWhiteSpace(Index))
				{
					foreach (var column in schema.Columns)
					{
						if (column.Name is null)
							continue;

						if (string.Equals(column.Index, Index, StringComparison.OrdinalIgnoreCase))
							columns.Add(column.Name);
					}
				}
				else
					columns.Add(Name);

				/*
				 * Verify that both indexes contain the same number of columns.
				 */
				if (existingColumns.Length != columns.Count)
					return false;

				/*
				 * Sort both column lists and compare element by element to ensure
				 * index composition matches regardless of declaration order.
				 */
				existingColumns = existingColumns.Sort();
				columns.Sort();

				for (var i = 0; i < existingColumns.Length; i++)
				{
					if (!string.Equals(existingColumns[i], columns[i], StringComparison.OrdinalIgnoreCase))
						return false;
				}
			}
		}
		else
			return string.Equals(Index, other.Index, StringComparison.Ordinal);

		return true;
	}
}
