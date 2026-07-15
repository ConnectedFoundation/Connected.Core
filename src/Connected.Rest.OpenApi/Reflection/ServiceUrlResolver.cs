using Connected.Annotations;
using Connected.Reflection;
using System.Reflection;
using System.Text;

namespace Connected.Net.Rest.OpenApi.Reflection;

/// <summary>
/// Resolves service and method urls the same way <c>Connected.Net.Rest.ResolutionService</c> does,
/// so the generated OpenAPI document matches the routes Connected.Core actually maps.
/// </summary>
internal static class ServiceUrlResolver
{
	public static string ResolveServiceUrl(Type type)
	{
		if (type.GetCustomAttribute<ServiceUrlAttribute>() is ServiceUrlAttribute attribute)
			return attribute.Url;

		return $"{PascalNamespace(type.Namespace)}/{type.Name.ToPascalCase()}".Replace('.', '/');
	}

	public static string ResolveMethodUrl(MethodInfo method)
	{
		if (method.GetCustomAttribute<ServiceUrlAttribute>() is ServiceUrlAttribute attribute)
			return attribute.Url;

		return method.Name.ToCamelCase();
	}

	private static string? PascalNamespace(string? @namespace)
	{
		if (string.IsNullOrEmpty(@namespace))
			return null;

		var tokens = @namespace.Split('.');
		var result = new StringBuilder();

		foreach (var token in tokens)
			result.Append($"{token.ToPascalCase()}.");

		return result.ToString().TrimEnd('.');
	}
}
