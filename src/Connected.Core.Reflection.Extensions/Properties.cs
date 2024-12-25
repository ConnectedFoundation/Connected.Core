using System.Reflection;
using System.Runtime.CompilerServices;

namespace Connected.Reflection;

public static class Properties
{
	public static PropertyInfo? GetPropertyAttribute<T>(object instance) where T : Attribute
	{
		var props = GetProperties(instance, false);

		if (props is null || !props.Any())
			return default;

		foreach (var property in props)
		{
			if (property.GetCustomAttribute<T>() is not null)
				return property;
		}

		return default;
	}

	public static PropertyInfo[]? GetProperties(object instance, bool writableOnly)
	{
		if (instance.GetType().GetProperties() is not PropertyInfo[] properties)
			return default;

		var temp = new List<PropertyInfo>();

		foreach (var i in properties)
		{
			var getMethod = i.GetGetMethod();
			var setMethod = i.GetSetMethod();

			if (writableOnly && setMethod is null)
				continue;

			if (getMethod is null)
				continue;

			if (getMethod is not null && getMethod.IsStatic || setMethod is not null && setMethod.IsStatic)
				continue;

			if (setMethod is not null && !setMethod.IsPublic)
				continue;

			temp.Add(i);
		}

		return temp.ToArray();
	}

	public static IEnumerable<PropertyInfo> GetInheritedProperites(this Type type)
	{
		foreach (var info in type.GetInheritedTypeInfos())
		{
			foreach (var p in info.GetRuntimeProperties())
				yield return p;
		}
	}

	public static List<PropertyInfo> GetImplementedProperties(object component)
	{
		var type = component is Type ct ? ct : component.GetType();
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
		var result = new List<PropertyInfo>();

		foreach (var property in properties)
		{
			if (property.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
				continue;

			result.Add(property);
		}

		return result;
	}

	public static void SetPropertyValue(object instance, string propertyName, object? value)
	{
		var property = instance.GetType().GetProperty(propertyName);

		if (property is null)
			return;

		if (!property.CanWrite)
		{
			if (property.DeclaringType is null)
				return;

			property = property.DeclaringType.GetProperty(propertyName);
		}

		if (property is null || property.SetMethod is null)
			return;

		if (value is null)
			property.SetMethod.Invoke(instance, null);
		else
			property.SetMethod.Invoke(instance, new object[] { value });
	}

	public static T? FindAttribute<T>(this PropertyInfo info) where T : Attribute
	{
		var atts = info.GetCustomAttributes<T>(true);

		if (atts is null || !atts.Any())
			return default;

		return atts.ElementAt(0);
	}
}
