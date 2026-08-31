using System.Reflection;

namespace Connected.Net.Rest.OpenApi.Documentation;

/// <summary>
/// Builds the member id strings the C# compiler writes into a project's generated XML documentation file
/// </summary>
internal static class MemberDocumentationId
{
	public static string? For(MemberInfo member)
	{
		return member switch
		{
			Type type => ForType(type),
			MethodInfo method => ForMethod(method),
			PropertyInfo property => ForProperty(property),
			FieldInfo field => ForField(field),
			_ => null
		};
	}

	private static string ForType(Type type)
	{
		return $"T:{TypeId(type)}";
	}

	private static string? ForProperty(PropertyInfo property)
	{
		if (property.DeclaringType is not Type declaringType)
			return null;

		return $"P:{TypeId(declaringType)}.{property.Name}";
	}

	private static string? ForField(FieldInfo field)
	{
		if (field.DeclaringType is not Type declaringType)
			return null;

		return $"F:{TypeId(declaringType)}.{field.Name}";
	}

	private static string? ForMethod(MethodInfo method)
	{
		if (method.DeclaringType is not Type declaringType)
			return null;

		var parameters = method.GetParameters();
		var signature = parameters.Length == 0
			? string.Empty
			: $"({string.Join(',', parameters.Select(f => ParameterTypeId(f.ParameterType)))})";

		var arity = method.IsGenericMethod
			? $"``{method.GetGenericArguments().Length}"
			: string.Empty;

		return $"M:{TypeId(declaringType)}.{method.Name}{arity}{signature}";
	}

	/// <summary>
	/// The id fragment identifying a type as the declaring type of a member.
	/// </summary>
	private static string TypeId(Type type)
	{
		if (type.IsGenericParameter)
			return $"`{type.GenericParameterPosition}";

		if (type.IsGenericType && !type.IsGenericTypeDefinition)
			return TypeId(type.GetGenericTypeDefinition());

		return (type.FullName ?? $"{type.Namespace}.{type.Name}").Replace('+', '.');
	}

	/// <summary>
	/// The id fragment for a type appearing as a method parameter
	/// </summary>
	private static string ParameterTypeId(Type type)
	{
		if (type.IsByRef && type.GetElementType() is Type byRefElement)
			return $"{ParameterTypeId(byRefElement)}@";

		if (type.IsArray && type.GetElementType() is Type arrayElement)
			return $"{ParameterTypeId(arrayElement)}[]";

		if (type.IsGenericParameter)
			return type.DeclaringMethod is not null ? $"``{type.GenericParameterPosition}" : $"`{type.GenericParameterPosition}";

		if (type.IsGenericType && !type.IsGenericTypeDefinition)
		{
			var definition = type.GetGenericTypeDefinition();
			var arguments = string.Join(',', type.GetGenericArguments().Select(ParameterTypeId));

			return $"{StripArity(TypeId(definition))}{{{arguments}}}";
		}

		return TypeId(type);
	}

	private static string StripArity(string name)
	{
		var index = name.IndexOf('`');

		return index < 0 ? name : name[..index];
	}
}
