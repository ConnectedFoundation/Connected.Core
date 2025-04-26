using System.Collections.Immutable;

namespace Connected;

public static class CoreExtensions
{
	private static List<Type> Middlewares { get; } = [];
	public static void AddMiddleware<T>(this IServiceCollection services)
	{
		Middlewares.Add(typeof(T));
		services.AddTransient(typeof(T));
	}

	public static void AddMiddleware(this IServiceCollection services, Type type)
	{
		Middlewares.Add(type);
		services.AddTransient(type);
	}

	public static void AddMiddleware(Type type)
	{
		Middlewares.Add(type);
	}

	public static IImmutableList<Type> QueryMiddlewares(this IConfiguration configuration)
	{
		return [.. Middlewares];
	}
}
