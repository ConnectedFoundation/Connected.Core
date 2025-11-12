namespace Connected;

/// <summary>
/// Provides extension methods for core middleware and service registration.
/// </summary>
/// <remarks>
/// This static class centralizes middleware registration and discovery capabilities,
/// maintaining a collection of registered middleware types that can be queried throughout
/// the application lifecycle. It supports both dependency injection-based and standalone
/// middleware registration patterns.
/// </remarks>
public static class CoreExtensions
{
	/// <summary>
	/// Gets the collection of registered middleware types.
	/// </summary>
	internal static List<Type> Middlewares { get; } = [];

	/// <summary>
	/// Registers a middleware type with the service collection using dependency injection.
	/// </summary>
	/// <typeparam name="T">The middleware type to register.</typeparam>
	/// <param name="services">The service collection to which the middleware is added.</param>
	/// <remarks>
	/// This method registers the middleware type with transient lifetime and adds it to
	/// the internal middleware collection for later discovery.
	/// </remarks>
	public static void AddMiddleware<T>(this IServiceCollection services)
	{
		Middlewares.Add(typeof(T));
		services.AddTransient(typeof(T));
	}

	/// <summary>
	/// Registers a middleware type with the service collection using dependency injection.
	/// </summary>
	/// <param name="services">The service collection to which the middleware is added.</param>
	/// <param name="type">The middleware type to register.</param>
	/// <remarks>
	/// This method registers the middleware type with transient lifetime and adds it to
	/// the internal middleware collection for later discovery.
	/// </remarks>
	public static void AddMiddleware(this IServiceCollection services, Type type)
	{
		Middlewares.Add(type);
		services.AddTransient(type);
	}
}
