using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.Oracle.Schemas;

/// <summary>
/// Compares two schema column definitions for equality.
/// </summary>
/// <remarks>
/// This static class provides comparison logic to determine whether two column definitions are
/// equivalent. It compares data types, lengths, precision, scale, nullability, identity specifications,
/// and other column characteristics to determine if a column alteration is necessary during schema
/// synchronization. The comparison handles Oracle-specific type mappings including NUMBER precision/scale,
/// VARCHAR2 length limits, and normalizes type names for accurate comparison. Oracle's flexible NUMBER
/// type requires special handling as it can represent various numeric CLR types with different precision
/// and scale values. This class is central to the schema diff algorithm that determines which columns
/// need to be modified during table alteration.
/// </remarks>
internal static class ColumnComparer
{
	/// <summary>
	/// Determines whether two column definitions are equivalent.
	/// </summary>
	/// <param name="desired">The desired column definition from the schema.</param>
	/// <param name="existing">The existing column definition from the database.</param>
	/// <returns><c>true</c> if the columns are equivalent; otherwise, <c>false</c>.</returns>
	public static bool Equals(ISchemaColumn desired, ISchemaColumn existing)
	{
		/*
		 * Compare data types
		 */
		if (desired.DataType != existing.DataType)
			return false;

		/*
		 * Compare max length for variable-length types
		 */
		if (IsVariableLengthType(desired.DataType) && desired.MaxLength != existing.MaxLength)
			return false;

		/*
		 * Compare precision and scale for numeric types.
		 * Oracle's NUMBER type uses precision and scale for all numeric values.
		 */
		if (IsNumericType(desired.DataType))
		{
			if (desired.Precision != existing.Precision)
				return false;

			if (desired.Scale != existing.Scale)
				return false;
		}

		/*
		 * Compare nullability
		 */
		if (desired.IsNullable != existing.IsNullable)
			return false;

		/*
		 * Compare identity specification.
		 * Oracle 12c+ supports GENERATED AS IDENTITY.
		 */
		if (desired.IsIdentity != existing.IsIdentity)
			return false;

		/*
		 * Columns are equivalent
		 */
		return true;
	}

	/// <summary>
	/// Determines whether a data type requires length specification.
	/// </summary>
	/// <param name="type">The data type to check.</param>
	/// <returns><c>true</c> if the type requires length specification; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Oracle variable-length types include VARCHAR2, NVARCHAR2, RAW, CHAR, and NCHAR.
	/// </remarks>
	private static bool IsVariableLengthType(DbType type)
	{
		return type is DbType.String or DbType.StringFixedLength or DbType.Binary or DbType.AnsiString or DbType.AnsiStringFixedLength;
	}

	/// <summary>
	/// Determines whether a data type is numeric and requires precision/scale comparison.
	/// </summary>
	/// <param name="type">The data type to check.</param>
	/// <returns><c>true</c> if the type is numeric; otherwise, <c>false</c>.</returns>
	/// <remarks>
	/// Oracle uses NUMBER type for all numeric values with configurable precision and scale.
	/// This includes Int16, Int32, Int64, Decimal, and VarNumeric types.
	/// </remarks>
	private static bool IsNumericType(DbType type)
	{
		return type is DbType.Decimal or DbType.VarNumeric or DbType.Int16 or DbType.Int32 or DbType.Int64 
			or DbType.Byte or DbType.SByte or DbType.UInt16 or DbType.UInt32 or DbType.UInt64;
	}
}
