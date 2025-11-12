using Connected.Data.Expressions.Languages;
using Oracle.ManagedDataAccess.Client;

namespace Connected.Storage.Oracle.Query;

/// <summary>
/// Represents an Oracle specific data type with its characteristics.
/// </summary>
/// <remarks>
/// This sealed class encapsulates Oracle data type information including the database type,
/// nullability constraints, length specifications, precision, and scale for numeric types.
/// It extends the base <see cref="DataType"/> class to provide Oracle-specific functionality.
/// The class uses OracleDbType enumeration to represent Oracle native types including NUMBER,
/// VARCHAR2, CLOB, BLOB, TIMESTAMP, RAW, and other Oracle-specific types. Oracle's NUMBER type
/// is highly flexible, supporting various precision and scale combinations for different numeric
/// requirements from integers to high-precision decimals.
/// </remarks>
internal sealed class OracleDataType
	: DataType
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OracleDataType"/> class with the specified characteristics.
	/// </summary>
	/// <param name="dbType">The Oracle database type.</param>
	/// <param name="notNull">Indicates whether the data type is non-nullable.</param>
	/// <param name="length">The maximum length for character or binary data types.</param>
	/// <param name="precision">The precision for numeric data types.</param>
	/// <param name="scale">The scale for numeric data types with decimal places.</param>
	public OracleDataType(OracleDbType dbType, bool notNull, int length, short precision, short scale)
	{
		DbType = dbType;
		NotNull = notNull;
		Length = length;
		Precision = precision;
		Scale = scale;
	}

	/// <summary>
	/// Gets the Oracle database type for this data type.
	/// </summary>
	public OracleDbType DbType { get; }
}
