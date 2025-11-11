namespace Connected.Storage.Schemas;

/// <summary>
/// Represents a column in a database table with detailed metadata.
/// </summary>
/// <remarks>
/// This interface provides comprehensive information about a table column as it exists
/// in the database, including data type details, constraints, character encoding, numeric
/// precision, and referential integrity information. Unlike <see cref="ISchemaColumn"/>
/// which represents the desired schema derived from entity models, this interface represents
/// the actual current state of a column in the database. It is used during schema inspection
/// and comparison operations to determine what changes need to be made to synchronize the
/// database structure with entity models. The interface includes detailed metadata about
/// character, numeric, and date/time columns, as well as foreign key relationships and
/// table constraints.
/// </remarks>
public interface ITableColumn
{
	/// <summary>
	/// Gets the column name.
	/// </summary>
	/// <value>
	/// A string representing the database column name.
	/// </value>
	/// <remarks>
	/// The column name uniquely identifies the column within its parent table.
	/// </remarks>
	string Name { get; }

	/// <summary>
	/// Gets the database-specific data type name.
	/// </summary>
	/// <value>
	/// A string representing the native database type (e.g., "nvarchar", "int", "datetime2").
	/// </value>
	/// <remarks>
	/// The data type string represents the actual database-specific type name as it appears
	/// in the database schema, which may differ from the DbType enumeration values.
	/// </remarks>
	string DataType { get; }

	/// <summary>
	/// Gets a value indicating whether this column is an identity (auto-increment) column.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column automatically generates values; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Identity columns generate sequential values automatically when new rows are inserted.
	/// </remarks>
	bool Identity { get; }

	/// <summary>
	/// Gets a value indicating whether the column allows null values.
	/// </summary>
	/// <value>
	/// <c>true</c> if the column can contain null values; otherwise, <c>false</c>.
	/// </value>
	/// <remarks>
	/// Nullable columns can store null values, while non-nullable columns require a value.
	/// </remarks>
	bool IsNullable { get; }

	/// <summary>
	/// Gets the default value constraint for the column.
	/// </summary>
	/// <value>
	/// A string representing the default value expression, or an empty string if no default exists.
	/// </value>
	/// <remarks>
	/// Default values are automatically applied when a new row is inserted without providing
	/// a value for this column. The expression is in database-specific SQL syntax.
	/// </remarks>
	string DefaultValue { get; }

	/// <summary>
	/// Gets the ordinal position of the column in the table.
	/// </summary>
	/// <value>
	/// An integer representing the column's position (1-based index).
	/// </value>
	/// <remarks>
	/// The ordinal position indicates the column's order within the table definition,
	/// which affects result set ordering when using SELECT * queries.
	/// </remarks>
	int Ordinal { get; }

	/// <summary>
	/// Gets the maximum length in characters for character data types.
	/// </summary>
	/// <value>
	/// An integer representing the maximum number of characters, or 0 if not applicable.
	/// </value>
	/// <remarks>
	/// For character types like varchar or nvarchar, this specifies the maximum number
	/// of characters the column can store. A value of -1 typically indicates MAX length.
	/// </remarks>
	int CharacterMaximumLength { get; }

	/// <summary>
	/// Gets the maximum length in bytes for character data types.
	/// </summary>
	/// <value>
	/// An integer representing the maximum number of bytes, or 0 if not applicable.
	/// </value>
	/// <remarks>
	/// For character types, this specifies the maximum number of bytes required to store
	/// the maximum number of characters, accounting for character encoding.
	/// </remarks>
	int CharacterOctetLength { get; }

	/// <summary>
	/// Gets the precision for numeric data types.
	/// </summary>
	/// <value>
	/// An integer representing the total number of significant digits, or 0 if not applicable.
	/// </value>
	/// <remarks>
	/// For numeric and decimal types, precision defines the total number of digits that
	/// can be stored, including both integer and fractional parts.
	/// </remarks>
	int NumericPrecision { get; }

	/// <summary>
	/// Gets the radix for numeric precision.
	/// </summary>
	/// <value>
	/// An integer representing the base for the precision (typically 10 for decimal types or 2 for binary types).
	/// </value>
	/// <remarks>
	/// The precision radix indicates whether numeric precision is specified in decimal
	/// digits (base 10) or binary bits (base 2).
	/// </remarks>
	int NumericPrecisionRadix { get; }

	/// <summary>
	/// Gets the scale for numeric data types.
	/// </summary>
	/// <value>
	/// An integer representing the number of digits to the right of the decimal point, or 0 if not applicable.
	/// </value>
	/// <remarks>
	/// For numeric and decimal types, scale defines how many fractional digits can be
	/// stored after the decimal point.
	/// </remarks>
	int NumericScale { get; }

	/// <summary>
	/// Gets the precision for date/time data types.
	/// </summary>
	/// <value>
	/// An integer representing the fractional seconds precision, or 0 if not applicable.
	/// </value>
	/// <remarks>
	/// For date/time types, this specifies the number of decimal places for fractional
	/// seconds, affecting timestamp accuracy.
	/// </remarks>
	int DateTimePrecision { get; }

	/// <summary>
	/// Gets the character set name for character data types.
	/// </summary>
	/// <value>
	/// A string representing the character encoding (e.g., "utf8", "latin1"), or an empty string if not applicable.
	/// </value>
	/// <remarks>
	/// The character set defines which character encoding is used to store character data,
	/// affecting collation and sorting behavior.
	/// </remarks>
	string CharacterSetName { get; }

	/// <summary>
	/// Gets the foreign key referential constraint for this column.
	/// </summary>
	/// <value>
	/// An <see cref="IReferentialConstraint"/> if the column has a foreign key, or null if none exists.
	/// </value>
	/// <remarks>
	/// The referential constraint defines the foreign key relationship between this column
	/// and another table's primary key, including cascade rules for updates and deletes.
	/// </remarks>
	IReferentialConstraint Reference { get; }

	/// <summary>
	/// Gets the collection of constraints defined on this column.
	/// </summary>
	/// <value>
	/// A list containing all constraint definitions for the column.
	/// </value>
	/// <remarks>
	/// Constraints include check constraints, unique constraints, and other validation
	/// rules that restrict the values that can be stored in the column.
	/// </remarks>
	List<ITableConstraint> Constraints { get; }
}