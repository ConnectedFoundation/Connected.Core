using System.ComponentModel;

namespace Connected.Reflection;
internal static class TypeConversion
{
	public static object Convert(object value, Type type)
	{
		if (value.GetType() == type)
			return value;

		var converter = TypeDescriptor.GetConverter(value);

		if (converter.CanConvertTo(type))
		{
			var result = converter.ConvertTo(value, type);

			return result is null ? throw new NullReferenceException() : result;
		}

		if (TryConvertEnum(value, type, out object? er) && er is not null)
			return er;

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
