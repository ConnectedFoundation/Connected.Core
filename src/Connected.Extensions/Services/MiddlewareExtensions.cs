using Connected.Annotations;
using System.Reflection;

namespace Connected.Services;

public static class MiddlewareExtensions
{
	public static List<Type> GetImplementedMiddlewares(this Type type)
	{
		var result = new List<Type>();

		if (typeof(IMiddleware).FullName is not string fullName)
			return result;

		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i.GetInterface(fullName) is not null)
				result.Add(i);
		}

		return result;
	}

	public static string? ResolveMiddlewareId(this Type type)
	{
		if (type.GetCustomAttribute<MiddlewareIdAttribute>() is MiddlewareIdAttribute attribute)
			return attribute.Id;

		return default;
	}

	public static string? ResolveMiddlewareId(this IMiddleware middleware)
	{
		return middleware?.GetType().ResolveMiddlewareId();
	}
}
