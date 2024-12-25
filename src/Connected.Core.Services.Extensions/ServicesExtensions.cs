using Connected.Annotations;
using Connected.Services.Middlewares;
using System.Reflection;

namespace Connected.Services;

public static class ServicesExtensions
{
	public static List<Type> GetImplementedDtos(this Type type)
	{
		/*
		 * Only direct implementation is used so we can eliminate multiple implementations
		 * and thus resolving wrong arguments when mapping request.
		 */
		var interfaces = type.GetInterfaces();
		var allInterfaces = new List<Type>();
		var baseInterfaces = new List<Type>();

		foreach (var i in interfaces)
		{
			if (typeof(IDto)?.FullName is not string fullName)
				continue;

			if (i.GetInterface(fullName) is null)
				continue;

			if (i == typeof(IDto))
				continue;

			allInterfaces.Add(i);

			foreach (var baseInterface in i.GetInterfaces())
			{
				if (baseInterface == typeof(IDto) || typeof(IDto)?.FullName is not string baseFullName)
					continue;

				if (baseInterface.GetInterface(baseFullName) is not null)
					baseInterfaces.Add(baseInterface);
			}
		}

		return allInterfaces.Except(baseInterfaces).ToList();
	}

	public static bool IsDtoImplementation(this Type type)
	{
		if (typeof(IDto)?.FullName is not string fullName)
			return false;

		return !type.IsInterface && !type.IsAbstract && type.GetInterface(fullName) is not null;
	}

	public static List<Type> GetImplementedServices(this Type type)
	{
		var result = new List<Type>();
		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i.GetCustomAttribute<ServiceAttribute>() is not null)
				result.Add(i);
		}

		return result;
	}

	public static string? ResolveServiceUrl(this Type type)
	{
		var services = type.GetImplementedServices();

		if (!services.Any())
			return null;

		foreach (var service in services)
		{
			var att = service.GetCustomAttribute<ServiceUrlAttribute>();

			if (att is not null)
				return att.Url;
		}

		return null;
	}

	public static bool IsService(this Type type)
	{
		var interfaces = type.GetInterfaces();

		foreach (var i in interfaces)
		{
			if (i.GetCustomAttribute<ServiceAttribute>() is not null)
				return true;
		}

		return false;
	}

	public static bool IsServiceOperationMiddleware(this Type type)
	{
		var f = typeof(IServiceOperationMiddleware).FullName;

		return f is not null && type.GetInterface(f) is not null;
	}

	public static bool IsServiceOperation(this Type type)
	{
		var f = typeof(IServiceOperation<>).FullName;

		return f is not null && type.GetInterface(f) is not null;
	}

	public static bool IsServiceFunction(this Type type)
	{
		var f = typeof(IFunction<,>).FullName;

		return (f is not null && type.GetInterface(f) is not null);
	}
}
