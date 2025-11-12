using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Connected.Reflection;

public static class Nullables
{
	public static bool IsNullable(this PropertyInfo property)
	{
		return IsNullable(property.PropertyType, property.DeclaringType, property.CustomAttributes);
	}

	public static bool IsNullable(this FieldInfo field)
	{
		return IsNullable(field.FieldType, field.DeclaringType, field.CustomAttributes);
	}

	public static bool IsNullable(this ParameterInfo parameter)
	{
		return IsNullable(parameter.ParameterType, parameter.Member, parameter.CustomAttributes);
	}

	private static bool IsNullable(this Type memberType, MemberInfo? declaringType, IEnumerable<CustomAttributeData> customAttributes)
	{
		if (memberType.IsValueType)
			return Nullable.GetUnderlyingType(memberType) is not null;

		var nullable = customAttributes.FirstOrDefault(x => string.Equals(x.AttributeType.FullName, "System.Runtime.CompilerServices.NullableAttribute", StringComparison.Ordinal));

		if (nullable is not null && nullable.ConstructorArguments.Count == 1)
		{
			var attributeArgument = nullable.ConstructorArguments[0];

			if (attributeArgument.ArgumentType == typeof(byte[]))
			{
				var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value!;

				if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
					return (byte)args[0].Value! == 2;
			}
			else if (attributeArgument.ArgumentType == typeof(byte))
				return (byte)attributeArgument.Value! == 2;
		}

		for (var type = declaringType; type is not null; type = type.DeclaringType)
		{
			var context = type.CustomAttributes.FirstOrDefault(x => string.Equals(x.AttributeType.FullName, "System.Runtime.CompilerServices.NullableContextAttribute", StringComparison.Ordinal));

			if (context != null && context.ConstructorArguments.Count == 1 && context.ConstructorArguments[0].ArgumentType == typeof(byte))
				return (byte)context.ConstructorArguments[0].Value! == 2;
		}

		return false;
	}

	public static bool IsNullable(this Type type)
	{
		if (type is null)
			return false;

		if (type == typeof(string))
			return true;

		if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			return true;

		return Nullable.GetUnderlyingType(type) is not null;
	}

	public static bool IsNullAssignable(this Type type)
	{
		return !type.GetTypeInfo().IsValueType || type.IsNullable();
	}

	public static Type GetNonNullableType(this Type type)
	{
		if (type == typeof(string))
			return typeof(string);

		if (type.IsNullable())
			return type.GetTypeInfo().GenericTypeArguments[0];

		return type;
	}

	public static Type GetNullAssignableType(this Type type)
	{
		if (!type.IsNullAssignable())
			return typeof(Nullable<>).MakeGenericType(type);

		return type;
	}

	public static ConstantExpression GetNullConstant(this Type type)
	{
		return Expression.Constant(null, type.GetNullAssignableType());
	}

	public static bool HasNonNullableProperties(this ParameterInfo parameter)
	{
		var props = parameter.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

		foreach (var property in props)
		{
			if (!property.IsNullable())
				return true;
		}

		return false;
	}
}
