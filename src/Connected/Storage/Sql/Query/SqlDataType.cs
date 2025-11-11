using Connected.Data.Expressions.Languages;
using System.Data;

namespace Connected.Storage.Sql.Query;

internal sealed class SqlDataType
	: DataType
{
	public SqlDataType(SqlDbType dbType, bool notNull, int length, short precision, short scale)
	{
		DbType = dbType;
		NotNull = notNull;
		Length = length;
		Precision = precision;
		Scale = scale;
	}

	public SqlDbType DbType { get; }
}
