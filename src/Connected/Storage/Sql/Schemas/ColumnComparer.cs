using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

/// <summary>
/// Provides equality comparison for schema columns.
/// </summary>
/// <remarks>
/// This comparer performs comprehensive equality checks on schema columns by comparing all
/// relevant properties including data types, constraints, indexes, precision, scale, and
/// default values. It is used during schema synchronization operations to detect changes
/// between existing and desired column definitions. The comparison is ordinal and exact,
/// ensuring that even minor differences in column characteristics are properly identified.
/// </remarks>
internal class ColumnComparer
	: IEqualityComparer<ISchemaColumn>
{
	/// <summary>
	/// Gets the default singleton instance of the column comparer.
	/// </summary>
	public static ColumnComparer Default => new();

	/// <inheritdoc/>
	public int GetHashCode(ISchemaColumn value)
	{
		return value.GetHashCode();
	}

	/// <summary>
	/// Determines whether two schema columns are equal.
	/// </summary>
	/// <param name="left">The first column to compare.</param>
	/// <param name="right">The second column to compare.</param>
	/// <returns>
	/// <c>true</c> if the columns are equal; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	/// This method performs a comprehensive property-by-property comparison of all column
	/// characteristics to determine equality. Any difference in data type, constraints,
	/// precision, nullability, or other properties results in inequality.
	/// </remarks>
	public bool Equals(ISchemaColumn? left, ISchemaColumn? right)
	{
		if (left is null || right is null)
			return false;

		if (left.BinaryKind != right.BinaryKind)
			return false;

		if (left.DataType != right.DataType)
			return false;

		if (left.DateKind != right.DateKind)
			return false;

		if (left.DatePrecision != right.DatePrecision)
			return false;

		if (!string.Equals(left.DefaultValue, right.DefaultValue, StringComparison.Ordinal))
			return false;

		if (!string.Equals(left.Index, right.Index, StringComparison.Ordinal))
			return false;

		if (left.IsIdentity != right.IsIdentity)
			return false;

		if (left.IsIndex != right.IsIndex)
			return false;

		if (left.IsNullable != right.IsNullable)
			return false;

		if (left.IsPrimaryKey != right.IsPrimaryKey)
			return false;

		if (left.IsUnique != right.IsUnique)
			return false;

		if (left.IsVersion != right.IsVersion)
			return false;

		if (left.MaxLength != right.MaxLength)
			return false;

		if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal))
			return false;

		if (left.Precision != right.Precision)
			return false;

		if (left.Scale != right.Scale)
			return false;

		return true;
	}
}