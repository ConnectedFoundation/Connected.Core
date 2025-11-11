using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;

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
	private static List<Type> Middlewares { get; } = [];

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

	/// <summary>
	/// Registers a middleware type without dependency injection.
	/// </summary>
	/// <param name="type">The middleware type to register.</param>
	/// <remarks>
	/// This method adds the middleware type to the internal collection for discovery
	/// without registering it with a service container. This is useful for middleware
	/// that is instantiated manually or through alternative mechanisms.
	/// </remarks>
	public static void AddMiddleware(Type type)
	{
		Middlewares.Add(type);
	}

	/// <summary>
	/// Queries all registered middleware types.
	/// </summary>
	/// <param name="configuration">The configuration instance (unused in current implementation).</param>
	/// <returns>An immutable list containing all registered middleware types.</returns>
	/// <remarks>
	/// This extension method provides access to the complete collection of middleware
	/// types that have been registered through any of the AddMiddleware methods.
	/// </remarks>
	public static IImmutableList<Type> QueryMiddlewares(this IConfiguration configuration)
	{
		return [.. Middlewares];
	}
}
