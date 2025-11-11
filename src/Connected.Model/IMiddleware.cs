namespace Connected;

/// <summary>
/// The base marker interface for all middleware implementations.
/// </summary>
/// <remarks>
/// This interface serves as the foundation for all middleware components in the application.
/// Middleware implementations must be disposable and support asynchronous initialization.
/// All middleware interfaces should inherit from this interface to ensure consistent
/// lifecycle management and initialization patterns across the platform.
/// </remarks>
public interface IMiddleware
	: IDisposable
{
	/// <summary>
	/// Asynchronously initializes the middleware component.
	/// </summary>
	/// <returns>A task that represents the asynchronous initialization operation.</returns>
	Task Initialize();
}
