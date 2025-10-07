using Connected.Storage.Schemas;

namespace Connected.Storage.Sql.Schemas;

internal class ColumnComparer : IEqualityComparer<ISchemaColumn>
{
	public static ColumnComparer Default => new();

	public int GetHashCode(ISchemaColumn value)
	{
		return value.GetHashCode();
	}

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