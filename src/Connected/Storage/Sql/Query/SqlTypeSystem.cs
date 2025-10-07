using Connected.Data.Expressions.Languages;
using Connected.Data.Expressions.TypeSystem;
using Connected.Reflection;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Connected.Storage.Sql.Query;

internal sealed class SqlTypeSystem : QueryTypeSystem
{
	public static int StringDefaultSize => int.MaxValue;
	public static int BinaryDefaultSize => int.MaxValue;

	public override DataType Parse(string typeDeclaration)
	{
		string[]? args = null;
		string typeName;
		string? remainder = null;
		var openParen = typeDeclaration.IndexOf('(');

		if (openParen >= 0)
		{
			typeName = typeDeclaration[..openParen].Trim();

			var closeParen = typeDeclaration.IndexOf(')', openParen);

			if (closeParen < openParen)
				closeParen = typeDeclaration.Length;

			var argstr = typeDeclaration[(openParen + 1)..closeParen];

			args = argstr.Split(',');

			remainder = typeDeclaration[(closeParen + 1)..];
		}
		else
		{
			var space = typeDeclaration.IndexOf(' ');

			if (space >= 0)
			{
				typeName = typeDeclaration[..space];
				remainder = typeDeclaration[(space + 1)..].Trim();
			}
			else
				typeName = typeDeclaration;
		}

		var isNotNull = remainder is not null && remainder.Contains("NOT NULL", StringComparison.CurrentCultureIgnoreCase);

		return ResolveDataType(typeName, args, isNotNull);
	}

	public static DataType ResolveDataType(string typeName, string[]? args, bool isNotNull)
	{
		if (string.Equals(typeName, "rowversion", StringComparison.OrdinalIgnoreCase))
			typeName = "Timestamp";

		if (string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase))
			typeName = "Decimal";

		if (string.Equals(typeName, "sql_variant", StringComparison.OrdinalIgnoreCase))
			typeName = "Variant";

		var dbType = ResolveSqlType(typeName);
		var length = 0;
		short precision = 0;
		short scale = 0;

		switch (dbType)
		{
			case SqlDbType.Binary:
			case SqlDbType.Char:
			case SqlDbType.Image:
			case SqlDbType.NChar:
			case SqlDbType.NVarChar:
			case SqlDbType.VarBinary:
			case SqlDbType.VarChar:
				length = args is null || args.Length == 0 ? 32 : string.Equals(args[0], "max", StringComparison.OrdinalIgnoreCase) ? int.MaxValue : int.Parse(args[0]);
				break;
			case SqlDbType.Money:
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				scale = args is null || args.Length < 2 ? (short)4 : short.Parse(args[1], NumberFormatInfo.InvariantInfo);
				break;
			case SqlDbType.Decimal:
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				scale = args is null || args.Length < 2 ? (short)0 : short.Parse(args[1], NumberFormatInfo.InvariantInfo);
				break;
			case SqlDbType.Float:
			case SqlDbType.Real:
				precision = args is null || args.Length == 0 ? (short)29 : short.Parse(args[0], NumberFormatInfo.InvariantInfo);
				break;
		}

		return NewType(dbType, isNotNull, length, precision, scale);
	}

	private static SqlDataType NewType(SqlDbType type, bool isNotNull, int length, short precision, short scale)
	{
		return new SqlDataType(type, isNotNull, length, precision, scale);
	}

	public static SqlDbType ResolveSqlType(string typeName)
	{
		return Enum.Parse<SqlDbType>(typeName, true);
	}

	public override DataType ResolveColumnType(Type type)
	{
		var isNotNull = type.GetTypeInfo().IsValueType && !type.IsNullable();
		type = Nullables.GetNonNullableType(type);

		switch (type.GetTypeCode())
		{
			case TypeCode.Boolean:
				return NewType(SqlDbType.Bit, isNotNull, 0, 0, 0);
			case TypeCode.SByte:
			case TypeCode.Byte:
				return NewType(SqlDbType.TinyInt, isNotNull, 0, 0, 0);
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return NewType(SqlDbType.SmallInt, isNotNull, 0, 0, 0);
			case TypeCode.Int32:
			case TypeCode.UInt32:
				return NewType(SqlDbType.Int, isNotNull, 0, 0, 0);
			case TypeCode.Int64:
			case TypeCode.UInt64:
				return NewType(SqlDbType.BigInt, isNotNull, 0, 0, 0);
			case TypeCode.Single:
			case TypeCode.Double:
				return NewType(SqlDbType.Float, isNotNull, 0, 0, 0);
			case TypeCode.String:
				return NewType(SqlDbType.NVarChar, isNotNull, StringDefaultSize, 0, 0);
			case TypeCode.Char:
				return NewType(SqlDbType.NChar, isNotNull, 1, 0, 0);
			case TypeCode.DateTime:
				return NewType(SqlDbType.DateTime, isNotNull, 0, 0, 0);
			case TypeCode.Decimal:
				return NewType(SqlDbType.Decimal, isNotNull, 0, 29, 4);
			default:
				if (type == typeof(byte[]))
					return NewType(SqlDbType.VarBinary, isNotNull, BinaryDefaultSize, 0, 0);
				else if (type == typeof(Guid))
					return NewType(SqlDbType.UniqueIdentifier, isNotNull, 0, 0, 0);
				else if (type == typeof(DateTimeOffset))
					return NewType(SqlDbType.DateTimeOffset, isNotNull, 0, 0, 0);
				else if (type == typeof(TimeSpan))
					return NewType(SqlDbType.Time, isNotNull, 0, 0, 0);
				else if (type.GetTypeInfo().IsEnum)
					return NewType(SqlDbType.Int, isNotNull, 0, 0, 0);
				else
					return NewType(SqlDbType.VarBinary, isNotNull, BinaryDefaultSize, 0, 0);
		}
	}

	public static bool IsVariableLength(SqlDbType dbType)
	{
		return dbType switch
		{
			SqlDbType.Image or SqlDbType.NText or SqlDbType.NVarChar or SqlDbType.Text or SqlDbType.VarBinary or SqlDbType.VarChar or SqlDbType.Xml => true,
			_ => false,
		};
	}

	public override string Format(DataType type, bool suppressSize)
	{
		var sqlType = (SqlDataType)type;
		var sb = new StringBuilder();

		sb.Append(sqlType.DbType.ToString().ToUpper());

		if (sqlType.Length > 0 && !suppressSize)
		{
			if (sqlType.Length == int.MaxValue)
				sb.Append("(max)");
			else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", sqlType.Length);
		}
		else if (sqlType.Precision != 0)
		{
			if (sqlType.Scale != 0)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0},{1})", sqlType.Precision, sqlType.Scale);
			else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "({0})", sqlType.Precision);
		}

		return sb.ToString();
	}
}