using System.Collections.Immutable;

namespace Connected;

public static class CoreUtils
{
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
		CoreExtensions.Middlewares.Add(type);
	}

	/// <summary>
	/// Queries all registered middleware types.
	/// </summary>
	/// <returns>An immutable list containing all registered middleware types.</returns>
	/// <remarks>
	/// This extension method provides access to the complete collection of middleware
	/// types that have been registered through any of the AddMiddleware methods.
	/// </remarks>
	public static IImmutableList<Type> QueryMiddlewares()
	{
		return [.. CoreExtensions.Middlewares];
	}
}
