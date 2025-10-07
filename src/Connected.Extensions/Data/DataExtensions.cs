using Connected.Annotations.Entities;
using Connected.Reflection;
using System.Data;
using System.Reflection;

namespace Connected.Data;

public static class Extensions
{
	/// <summary>
	/// Sets <see cref="StorageConnectionMode.Isolated"/> value to the <see cref="IConnectionProvider"/> on the
	/// provided <see cref="IContext"/>.
	/// </summary>
	/// <param name="context">The <see cref="IContext"/> to set the <see cref="StorageConnectionMode.Isolated"/> value.</param>
	// public static void UseIsolatedConnections(this IContext context)
	// {
	// 	if (context.GetService<IConnectionProvider>() is IConnectionProvider provider)
	// 		provider.Mode = StorageConnectionMode.Isolated;
	// }

	public static DbType ToDbType(this PropertyInfo property)
	{
		Type type;

		if (property.PropertyType.IsNullable())
			type = property.PropertyType.GetGenericArguments()[0];
		else
			type = property.PropertyType;

		if (type.IsEnum)
			type = Enum.GetUnderlyingType(type);

		if (type == typeof(char) || type == typeof(string))
		{
			if (property.FindAttribute<ETagAttribute>() is not null)
				return DbType.Binary;

			var str = property.FindAttribute<StringAttribute>();

			if (str is null)
				return DbType.String;

			return str.Kind switch
			{
				StringKind.NVarChar => DbType.String,
				StringKind.VarChar => DbType.AnsiString,
				StringKind.Char => DbType.AnsiStringFixedLength,
				StringKind.NChar => DbType.StringFixedLength,
				_ => DbType.String,
			};
		}
		else if (type == typeof(byte))
			return DbType.Byte;
		else if (type == typeof(bool))
			return DbType.Boolean;
		else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
		{
			var att = property.FindAttribute<DateAttribute>();

			if (att is null)
				return DbType.DateTime2;

			return att.Kind switch
			{
				DateKind.Date => DbType.Date,
				DateKind.DateTime => DbType.DateTime,
				DateKind.DateTime2 => DbType.DateTime2,
				DateKind.SmallDateTime => DbType.DateTime,
				DateKind.Time => DbType.Time,
				_ => DbType.DateTime2,
			};
		}
		else if (type == typeof(decimal))
			return DbType.Decimal;
		else if (type == typeof(double))
			return DbType.Double;
		else if (type == typeof(Guid))
			return DbType.Guid;
		else if (type == typeof(short))
			return DbType.Int16;
		else if (type == typeof(int))
			return DbType.Int32;
		else if (type == typeof(long))
			return DbType.Int64;
		else if (type == typeof(sbyte))
			return DbType.SByte;
		else if (type == typeof(float))
			return DbType.Single;
		else if (type == typeof(TimeSpan))
			return DbType.Time;
		else if (type == typeof(ushort))
			return DbType.UInt16;
		else if (type == typeof(uint))
			return DbType.UInt32;
		else if (type == typeof(ulong))
			return DbType.UInt64;
		else if (type == typeof(byte[]))
			return DbType.Binary;
		else
			return DbType.Binary;
	}
}
