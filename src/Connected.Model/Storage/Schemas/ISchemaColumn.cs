using Connected.Annotations.Entities;
using System.Data;
using System.Reflection;

namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a column definition in a database schema.
/// </summary>
/// <remarks>
/// This interface provides comprehensive metadata about a database column including its
/// data type, constraints, indexing properties, and relationship to entity properties.
/// Schema columns bridge the gap between entity model properties and physical database
/// columns, enabling schema synchronization, validation, and migration operations. The
/// interface includes information about primary keys, indexes, uniqueness constraints,
/// nullability, default values, and numeric or date/time precision settings. This metadata
/// is essential for generating DDL statements and ensuring database structure matches
/// the application's entity model definitions.
/// </remarks>
public interface ISchemaColumn
{
	/// <summary>
	/// Gets the column name.
	/// </summary>
	/// <value>
	/// A string representing the database column name.
	/// </value>
	/// <remarks>
	/// The column name identifies the column within its parent table or schema object.
	/// </remarks>
	string Name { get; }

	/// <summary>
	/// Gets the data type of the column.
	/// </summary>
	/// <value>
	/// A <see cref="DbType"/> value representing the database data type.
	/// </value>
	/// <remarks>
	/// The data type determines how values are stored and interpreted in the database,
	/// corresponding to standard database types like Int32, String, DateTime, etc.
	/// </remarks>
	DbType DataType { get; }

	/// <summary>
	/// Gets a value indicating whether this column is an identity (auto-increment) column.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column is an identity column; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Identity columns automatically generate sequential values when new rows are inserted,
	/// typically used for primary key columns.
	/// </remarks>
	bool IsIdentity { get; }

	/// <summary>
	/// Gets a value indicating whether this column has a unique constraint.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column must contain unique values; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Unique constraints ensure that all values in the column are distinct across rows,
	/// preventing duplicate entries.
	/// </remarks>
	bool IsUnique { get; }

	/// <summary>
	/// Gets a value indicating whether this column is indexed.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column is part of an index; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Indexed columns benefit from improved query performance for search and join operations.
	/// </remarks>
	bool IsIndex { get; }

	/// <summary>
	/// Gets a value indicating whether this column is part of the primary key.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column is a primary key column; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Primary key columns uniquely identify each row in the table and cannot contain null values.
	/// </remarks>
	bool IsPrimaryKey { get; }

	/// <summary>
	/// Gets a value indicating whether this column is used for optimistic concurrency control.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column is a version/rowversion column; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Version columns are automatically updated on each row modification and are used to
	/// detect concurrent update conflicts in optimistic concurrency scenarios.
	/// </remarks>
	bool IsVersion { get; }

	/// <summary>
	/// Gets the default value expression for the column.
	/// </summary>
	/// <value>
	/// A string representing the default value SQL expression, or null if no default is defined.
	/// </value>
	/// <remarks>
	/// Default values are automatically assigned to the column when a new row is inserted
	/// without explicitly providing a value for this column.
	/// </remarks>
	string? DefaultValue { get; }

	/// <summary>
	/// Gets the maximum length for character or binary columns.
	/// </summary>
	/// <value>
	/// An integer representing the maximum length in characters or bytes, or 0 if not applicable.
	/// </value>
	/// <remarks>
	/// For string columns, this represents the maximum number of characters. For binary
	/// columns, it represents the maximum number of bytes.
	/// </remarks>
	int MaxLength { get; }

	/// <summary>
	/// Gets a value indicating whether the column allows null values.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column can contain null values; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Nullable columns can store null values, while non-nullable columns require a value
	/// for every row.
	/// </remarks>
	bool IsNullable { get; }

	/// <summary>
	/// Gets the name of the index that this column participates in.
	/// </summary>
	/// <value>
	/// A string representing the index name, or null if the column is not indexed or uses a default index.
	/// </value>
	/// <remarks>
	/// This property identifies custom named indexes that include this column, enabling
	/// explicit index management and optimization.
	/// </remarks>
	string? Index { get; }

	/// <summary>
	/// Gets the scale for numeric columns.
	/// </summary>
	/// <value>
	/// An integer representing the number of digits to the right of the decimal point.
	/// </value>
	/// <remarks>
	/// Scale is used with decimal and numeric data types to specify precision of fractional
	/// values, such as 2 for currency values.
	/// </remarks>
	int Scale { get; }

	/// <summary>
	/// Gets the precision for numeric columns.
	/// </summary>
	/// <value>
	/// An integer representing the total number of significant digits.
	/// </value>
	/// <remarks>
	/// Precision defines the total number of digits that can be stored in a numeric column,
	/// including both integer and fractional parts.
	/// </remarks>
	int Precision { get; }

	/// <summary>
	/// Gets the date/time kind for temporal columns.
	/// </summary>
	/// <value>
	/// A <see cref="DateKind"/> value specifying how date/time values are interpreted.
	/// </value>
	/// <remarks>
	/// The date kind determines whether date/time values are treated as UTC, local time,
	/// or unspecified, affecting time zone conversions and interpretation.
	/// </remarks>
	DateKind DateKind { get; }

	/// <summary>
	/// Gets the binary storage kind for binary columns.
	/// </summary>
	/// <value>
	/// A <see cref="BinaryKind"/> value specifying how binary data is stored.
	/// </value>
	/// <remarks>
	/// The binary kind determines the storage format and handling of binary data,
	/// such as fixed-length binary or variable-length varbinary.
	/// </remarks>
	BinaryKind BinaryKind { get; }

	/// <summary>
	/// Gets the precision for date/time columns.
	/// </summary>
	/// <value>
	/// An integer representing the fractional seconds precision.
	/// </value>
	/// <remarks>
	/// Date precision specifies the number of decimal places for fractional seconds in
	/// date/time values, affecting timestamp accuracy.
	/// </remarks>
	int DatePrecision { get; }

	/// <summary>
	/// Gets the entity property that this column maps to.
	/// </summary>
	/// <value>
	/// A <see cref="PropertyInfo"/> representing the entity class property.
	/// </value>
	/// <remarks>
	/// The property provides the reflection information linking this database column to
	/// its corresponding property in the entity model, enabling mapping and data binding
	/// operations.
	/// </remarks>
	PropertyInfo? Property { get; }
}
