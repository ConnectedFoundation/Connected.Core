using Connected.Data.Expressions.Languages;
using System.Data;

namespace Connected.Storage.Sql.Query;

/// <summary>
/// Represents a SQL Server specific data type with its characteristics.
/// </summary>
/// <remarks>
/// This sealed class encapsulates SQL Server data type information including the database type,
/// nullability constraints, length specifications, precision, and scale for numeric types.
/// It extends the base <see cref="DataType"/> class to provide SQL Server-specific functionality.
/// </remarks>
internal sealed class SqlDataType
	: DataType
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SqlDataType"/> class with the specified characteristics.
	/// </summary>
	/// <param name="dbType">The SQL Server database type.</param>
	/// <param name="notNull">Indicates whether the data type is non-nullable.</param>
	/// <param name="length">The maximum length for character or binary data types.</param>
	/// <param name="precision">The precision for numeric data types.</param>
	/// <param name="scale">The scale for numeric data types with decimal places.</param>
	public SqlDataType(SqlDbType dbType, bool notNull, int length, short precision, short scale)
	{
		DbType = dbType;
		NotNull = notNull;
		Length = length;
		Precision = precision;
		Scale = scale;
	}

	/// <summary>
	/// Gets the SQL Server database type for this data type.
	/// </summary>
	public SqlDbType DbType { get; }
}
