using Connected.Annotations;
using System.Collections.Immutable;

namespace Connected;

/// <summary>
/// Provides service operations for discovering and retrieving middleware instances.
/// </summary>
/// <remarks>
/// This service interface enables dynamic middleware discovery and instantiation throughout
/// the application. It supports both generic and non-generic access patterns, allowing
/// retrieval of the first matching middleware or all middleware instances of a specific type.
/// Middleware can be filtered by optional identifiers or caller context to support
/// conditional middleware resolution based on runtime requirements.
/// </remarks>
[Service]
public interface IMiddlewareService
{
	/// <summary>
	/// Asynchronously retrieves the first middleware instance of the specified type.
	/// </summary>
	/// <typeparam name="TMiddleware">The type of the middleware to retrieve.</typeparam>
	/// <returns>A task that represents the asynchronous operation. The task result contains the first middleware instance of the specified type, or null if no instance is found.</returns>
	Task<TMiddleware?> First<TMiddleware>() where TMiddleware : IMiddleware;

	/// <summary>
	/// Asynchronously retrieves the first middleware instance of the specified type with an optional identifier.
	/// </summary>
	/// <typeparam name="TMiddleware">The type of the middleware to retrieve.</typeparam>
	/// <param name="id">The optional identifier used to filter middleware instances.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the first middleware instance matching the specified type and identifier, or null if no instance is found.</returns>
	Task<TMiddleware?> First<TMiddleware>(string? id) where TMiddleware : IMiddleware;

	/// <summary>
	/// Asynchronously queries all middleware instances of the specified type.
	/// </summary>
	/// <typeparam name="TMiddleware">The type of the middleware to query.</typeparam>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of all middleware instances of the specified type.</returns>
	Task<IImmutableList<TMiddleware>> Query<TMiddleware>() where TMiddleware : IMiddleware;

	/// <summary>
	/// Asynchronously queries all middleware instances of the specified type filtered by caller context.
	/// </summary>
	/// <typeparam name="TMiddleware">The type of the middleware to query.</typeparam>
	/// <param name="caller">The optional caller context used to filter middleware instances based on the operation's initiator.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of middleware instances matching the specified type and caller context.</returns>
	Task<IImmutableList<TMiddleware>> Query<TMiddleware>(ICallerContext? caller) where TMiddleware : IMiddleware;

	/// <summary>
	/// Asynchronously retrieves the first middleware instance of the specified type using non-generic access.
	/// </summary>
	/// <param name="type">The type of the middleware to retrieve.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the first middleware instance of the specified type, or null if no instance is found.</returns>
	Task<IMiddleware?> First(Type type);

	/// <summary>
	/// Asynchronously retrieves the first middleware instance of the specified type with an optional identifier using non-generic access.
	/// </summary>
	/// <param name="type">The type of the middleware to retrieve.</param>
	/// <param name="id">The optional identifier used to filter middleware instances.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the first middleware instance matching the specified type and identifier, or null if no instance is found.</returns>
	Task<IMiddleware?> First(Type type, string? id);

	/// <summary>
	/// Asynchronously queries all middleware instances of the specified type using non-generic access.
	/// </summary>
	/// <param name="type">The type of the middleware to query.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of all middleware instances of the specified type.</returns>
	Task<IImmutableList<IMiddleware>> Query(Type type);

	/// <summary>
	/// Asynchronously queries all middleware instances of the specified type filtered by caller context using non-generic access.
	/// </summary>
	/// <param name="type">The type of the middleware to query.</param>
	/// <param name="caller">The optional caller context used to filter middleware instances based on the operation's initiator.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains an immutable list of middleware instances matching the specified type and caller context.</returns>
	Task<IImmutableList<IMiddleware>> Query(Type type, ICallerContext? caller);
}