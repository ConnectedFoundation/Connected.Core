using Connected.Annotations.Entities;
using Connected.Reflection;
using System.Reflection;

namespace Connected.Data.Expressions.Reflection;

internal static class ReflectionExtensions
{
	public static bool IsInQueryable(this MethodInfo method)
	{
		return method.DeclaringType == typeof(Queryable) || method.DeclaringType == typeof(Enumerable);
	}

	public static object? GetValue(this MemberInfo member, object instance)
	{
		var pi = member as PropertyInfo;

		if (pi is not null)
			return pi.GetValue(instance, null);

		var fi = member as FieldInfo;

		if (fi is not null)
			return fi.GetValue(instance);

		throw new InvalidOperationException();
	}

	public static void SetValue(this MemberInfo member, object instance, object value)
	{
		var pi = member as PropertyInfo;

		if (pi is not null)
		{
			pi.SetValue(instance, value, null);

			return;
		}

		var fi = member as FieldInfo;

		if (fi is not null)
		{
			fi.SetValue(instance, value);

			return;
		}

		throw new InvalidOperationException();
	}

	public static string MappingId(this Type type)
	{
		var att = type.ResolveTableAttribute();

		return $"{att.Schema}.{att.Name}";
	}

	public static TableAttribute ResolveTableAttribute(this Type type)
	{
		var tableAttribute = type.GetCustomAttribute<TableAttribute>();

		tableAttribute ??= new TableAttribute { Name = type.Name.ToCamelCase(), Schema = SchemaAttribute.DefaultSchema };

		if (string.IsNullOrWhiteSpace(tableAttribute.Name))
			tableAttribute.Name = type.Name.ToCamelCase();

		if (string.IsNullOrWhiteSpace(tableAttribute.Schema))
			tableAttribute.Schema = SchemaAttribute.DefaultSchema;

		return tableAttribute;
	}
}
