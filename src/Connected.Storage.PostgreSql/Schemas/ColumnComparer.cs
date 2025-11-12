using Connected.Storage.Schemas;
using System.Data;

namespace Connected.Storage.PostgreSql.Schemas;

/// <summary>
/// Compares two schema column definitions for equality.
/// </summary>
/// <remarks>
/// This static class provides comparison logic to determine whether two column definitions are
/// equivalent. It compares data types, lengths, nullability, identity specifications, and other
/// column characteristics to determine if a column alteration is necessary during schema
/// synchronization. The comparison handles PostgreSQL-specific type mappings and normalizes
/// type names for accurate comparison. This class is central to the schema diff algorithm
/// that determines which columns need to be modified during table alteration.
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
		 * Compare nullability
		 */
		if (desired.IsNullable != existing.IsNullable)
			return false;

		/*
		 * Compare identity specification
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
	private static bool IsVariableLengthType(DbType type)
	{
		return type is DbType.String or DbType.StringFixedLength or DbType.Binary or DbType.AnsiString or DbType.AnsiStringFixedLength;
	}
}
