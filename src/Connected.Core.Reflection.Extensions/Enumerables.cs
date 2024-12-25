using System.Reflection;

namespace Connected.Reflection;

public static class Enumerables
{
	public static Type? FindEnumerable(this Type type)
	{
		if (type is null || type == typeof(string))
			return default;

		if (type.IsArray)
		{
			var elementType = type.GetElementType();

			if (elementType is not null)
				return typeof(IEnumerable<>).MakeGenericType(elementType);
			else
				return default;
		}

		var typeInfo = type.GetTypeInfo();

		if (typeInfo.IsGenericType)
		{
			foreach (var arg in typeInfo.GenericTypeArguments)
			{
				var en = typeof(IEnumerable<>).MakeGenericType(arg);

				if (en.GetTypeInfo().IsAssignableFrom(typeInfo))
					return en;
			}
		}

		foreach (var itf in typeInfo.ImplementedInterfaces)
		{
			var en = itf.FindEnumerable();

			if (en is not null)
				return en;
		}

		if (typeInfo.BaseType is not null && typeInfo.BaseType != typeof(object))
			return typeInfo.BaseType.FindEnumerable();

		return default;
	}

	public static bool IsEnumerable(this Type type)
	{
		return type.FindEnumerable() is not null;
	}

	public static bool IsDictionary(this Type type)
	{
		var typeName = typeof(System.Collections.IDictionary).FullName;

		if (typeName is null)
			return false;

		return type.GetInterface(typeName) is not null;
	}

	public static Type GetEnumerableType(this Type elementType)
	{
		return typeof(IEnumerable<>).MakeGenericType(elementType);
	}

	public static Type? GetEnumerableElementType(this Type? enumerableType)
	{
		if (enumerableType is null)
			return default;

		var en = enumerableType.FindEnumerable();

		if (en is null)
			return enumerableType;

		return en.GetTypeInfo().GenericTypeArguments[0];
	}
}
