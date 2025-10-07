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
}
