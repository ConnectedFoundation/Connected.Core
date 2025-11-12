using Connected;
using System;
using System.Reflection;

namespace Connected.Reflection.Merging;

internal static class PropertyResolver
{
	public static PropertyInfo? Resolve(Type type, string propertyName)
	{
		if (type.GetProperty(propertyName) is PropertyInfo property)
			return property;

		if (type.GetProperty(propertyName.ToCamelCase()) is PropertyInfo camelProperty)
			return camelProperty;

		if (type.GetProperty(propertyName.ToPascalCase()) is PropertyInfo pascalProperty)
			return pascalProperty;

		return null;
	}
}
