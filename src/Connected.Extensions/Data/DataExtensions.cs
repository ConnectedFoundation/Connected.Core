using Connected.Annotations.Entities;
using Connected.Reflection;
using System.Data;
using System.Reflection;

namespace Connected.Data;

/// <summary>
/// Provides data-related extension utilities for mapping runtime metadata
/// to database-specific types and behaviors.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Maps a CLR property type and its data annotations to an appropriate <see cref="DbType"/>.
	/// </summary>
	/// <param name="property">The reflected property to evaluate.</param>
	/// <returns>A <see cref="DbType"/> corresponding to the property's storage representation.</returns>
	public static DbType ToDbType(this PropertyInfo property)
	{
		/*
		 * Determine the effective CLR type: unwrap nullable<T>, enums map to their
		 * underlying integer type to ensure proper storage mapping.
		 */
		Type type;

		if (property.PropertyType.IsNullable())
			type = property.PropertyType.GetGenericArguments()[0];
		else
			type = property.PropertyType;

		if (type.IsEnum)
			type = Enum.GetUnderlyingType(type);

		/*
		 * Strings and chars may be influenced by ETag and String annotations.
		 * - ETag implies binary storage for concurrency tokens.
		 * - StringAttribute selects ANSI/Unicode and fixed/variable length kinds.
		 */
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
		/*
		 * Primitive numeric and boolean mappings follow the straightforward
		 * CLR-to-DbType correspondence.
		 */
		else if (type == typeof(byte))
			return DbType.Byte;
		else if (type == typeof(bool))
			return DbType.Boolean;
		/*
		 * Date/Time types can be refined by DateAttribute to specify dialect
		 * specific kinds. Default to DateTime2 for precision.
		 */
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
				DateKind.Offset => DbType.DateTimeOffset,
				_ => DbType.DateTime2,
			};
		}
		/*
		 * Remaining well-known frameworks types.
		 */
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

		/*
		 * Fallback to binary for unknown types to avoid runtime failures while
		 * still providing a storable representation.
		 */
		else
			return DbType.Binary;
	}
}
