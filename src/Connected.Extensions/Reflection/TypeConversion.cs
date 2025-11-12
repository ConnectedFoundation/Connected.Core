using System.ComponentModel;

namespace Connected.Reflection;
internal static class TypeConversion
{
	public static object? Convert(object value, Type type)
	{
		if (value.GetType() == type)
			return value;

		var valueConverter = TypeDescriptor.GetConverter(value);

		if (valueConverter.CanConvertTo(type))
			return valueConverter.ConvertTo(value, type);

		var typeConverter = TypeDescriptor.GetConverter(type);

		if (typeConverter.CanConvertFrom(value.GetType()))
			return typeConverter.ConvertFrom(value);

		if (TryConvertEnum(value, type, out object? er) && er is not null)
			return er;

		if (type.IsNullable())
		{
			var nonNullable = type.GetNonNullableType();

			return Convert(value, nonNullable);
		}

		if (value.GetType().IsAssignableTo(type))
			return value;

		return System.Convert.ChangeType(value, type);
	}

	private static bool TryConvertEnum(object value, Type type, out object? result)
	{
		result = null;

		if (!type.IsEnum)
			return false;

		if (Enum.TryParse(type, value.ToString(), out result))
			return true;

		return false;
	}
}
