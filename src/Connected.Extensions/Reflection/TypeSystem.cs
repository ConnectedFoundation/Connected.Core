using System.Data;
using System.Reflection;

namespace Connected.Reflection;

public static class TypeSystem
{
	public static bool IsInteger(this Type type)
	{
		var nnType = type.GetNonNullableType();

		return GetTypeCode(nnType) switch
		{
			TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
			_ => false,
		};
	}

	public static TypeCode GetTypeCode(this Type type)
	{
		if (type.IsEnum)
			return GetTypeCode(Enum.GetUnderlyingType(type));

		if (type == typeof(bool))
			return TypeCode.Boolean;
		else if (type == typeof(byte))
			return TypeCode.Byte;
		else if (type == typeof(sbyte))
			return TypeCode.SByte;
		else if (type == typeof(short))
			return TypeCode.Int16;
		else if (type == typeof(ushort))
			return TypeCode.UInt16;
		else if (type == typeof(int))
			return TypeCode.Int32;
		else if (type == typeof(uint))
			return TypeCode.UInt32;
		else if (type == typeof(long))
			return TypeCode.Int64;
		else if (type == typeof(ulong))
			return TypeCode.UInt64;
		else if (type == typeof(float))
			return TypeCode.Single;
		else if (type == typeof(double))
			return TypeCode.Double;
		else if (type == typeof(decimal))
			return TypeCode.Decimal;
		else if (type == typeof(string))
			return TypeCode.String;
		else if (type == typeof(char))
			return TypeCode.Char;
		else if (type == typeof(DateTime))
			return TypeCode.DateTime;
		else if (type == typeof(DateTimeOffset))
			return TypeCode.DateTime;
		else if (type == typeof(Guid))
			return TypeCode.String;
		else
			return TypeCode.Object;
	}

	public static Type ToType(this DbType type)
	{
		return type switch
		{
			DbType.AnsiString or DbType.AnsiStringFixedLength or DbType.String or DbType.StringFixedLength or DbType.Xml => typeof(string),
			DbType.Binary or DbType.Object => typeof(object),
			DbType.Boolean => typeof(bool),
			DbType.Byte => typeof(byte),
			DbType.Int16 => typeof(short),
			DbType.Int32 => typeof(int),
			DbType.SByte => typeof(sbyte),
			DbType.UInt16 => typeof(ushort),
			DbType.UInt32 => typeof(uint),
			DbType.Int64 => typeof(long),
			DbType.UInt64 => typeof(ulong),
			DbType.Currency or DbType.Decimal => typeof(decimal),
			DbType.Double => typeof(double),
			DbType.Single => typeof(float),
			DbType.VarNumeric => typeof(decimal),
			DbType.Date or DbType.DateTime or DbType.DateTime2 or DbType.Time => typeof(DateTime),
			DbType.DateTimeOffset => typeof(DateTimeOffset),
			DbType.Guid => typeof(Guid),
			_ => throw new NotSupportedException(),
		};
	}

	public static DbType ToDbType(this Type? type)
	{
		if (type is null)
			return DbType.Object;

		var underlyingType = type;

		if (underlyingType.IsEnum)
			underlyingType = Enum.GetUnderlyingType(underlyingType);
		else if (underlyingType.IsNullable())
			underlyingType = Nullable.GetUnderlyingType(underlyingType);

		if (underlyingType == typeof(char) || underlyingType == typeof(string))
			return DbType.String;
		else if (underlyingType == typeof(byte))
			return DbType.Byte;
		else if (underlyingType == typeof(bool))
			return DbType.Boolean;
		else if (underlyingType == typeof(DateTime))
			return DbType.DateTime2;
		else if (underlyingType == typeof(DateTimeOffset))
			return DbType.DateTimeOffset;
		else if (underlyingType == typeof(decimal))
			return DbType.Decimal;
		else if (underlyingType == typeof(double))
			return DbType.Double;
		else if (underlyingType == typeof(Guid))
			return DbType.Guid;
		else if (underlyingType == typeof(short))
			return DbType.Int16;
		else if (underlyingType == typeof(int))
			return DbType.Int32;
		else if (underlyingType == typeof(long))
			return DbType.Int64;
		else if (underlyingType == typeof(sbyte))
			return DbType.SByte;
		else if (underlyingType == typeof(float))
			return DbType.Single;
		else if (underlyingType == typeof(TimeSpan))
			return DbType.Time;
		else if (underlyingType == typeof(ushort))
			return DbType.UInt16;
		else if (underlyingType == typeof(uint))
			return DbType.UInt32;
		else if (underlyingType == typeof(ulong))
			return DbType.UInt64;
		else if (underlyingType == typeof(byte[]))
			return DbType.Binary;
		else
			return DbType.String;
	}

	public static bool IsIndexer(this PropertyInfo property)
	{
		return property.GetIndexParameters().Length != 0;
	}
}
