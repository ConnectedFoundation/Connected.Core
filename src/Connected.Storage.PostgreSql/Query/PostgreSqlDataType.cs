using Connected.Data.Expressions.Languages;
using NpgsqlTypes;

namespace Connected.Storage.PostgreSql.Query;

/// <summary>
/// Represents a PostgreSQL specific data type with its characteristics.
/// </summary>
/// <remarks>
/// This sealed class encapsulates PostgreSQL data type information including the database type,
/// nullability constraints, length specifications, precision, and scale for numeric types.
/// It extends the base <see cref="DataType"/> class to provide PostgreSQL-specific functionality.
/// The class uses NpgsqlDbType enumeration to represent PostgreSQL native types.
/// </remarks>
internal sealed class PostgreSqlDataType
	: DataType
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PostgreSqlDataType"/> class with the specified characteristics.
	/// </summary>
	/// <param name="dbType">The PostgreSQL database type.</param>
	/// <param name="notNull">Indicates whether the data type is non-nullable.</param>
	/// <param name="length">The maximum length for character or binary data types.</param>
	/// <param name="precision">The precision for numeric data types.</param>
	/// <param name="scale">The scale for numeric data types with decimal places.</param>
	public PostgreSqlDataType(NpgsqlDbType dbType, bool notNull, int length, short precision, short scale)
	{
		DbType = dbType;
		NotNull = notNull;
		Length = length;
		Precision = precision;
		Scale = scale;
	}

	/// <summary>
	/// Gets the PostgreSQL database type for this data type.
	/// </summary>
	public NpgsqlDbType DbType { get; }
}
