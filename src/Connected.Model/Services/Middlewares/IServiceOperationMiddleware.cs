namespace Connected.Services.Middlewares;

/// <summary>
/// Represents the base marker interface for all service operation middleware.
/// </summary>
/// <remarks>
/// This interface serves as the foundation for middleware components that intercept and process
/// service operations. It extends the middleware contract to identify components specifically
/// designed to work with service operations such as actions and functions. Implementations should
/// provide specific middleware logic for different operation types.
/// </remarks>
public interface IServiceOperationMiddleware
	: IMiddleware
{
}
