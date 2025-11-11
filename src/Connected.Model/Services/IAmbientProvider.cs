namespace Connected.Services;

/// <summary>
/// Provides ambient context values for service operations.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object for which ambient values are provided.</typeparam>
/// <remarks>
/// Ambient providers are middleware components that supply contextual information to DTOs
/// before they are processed by service operations. They enable injection of cross-cutting
/// concerns such as security context, tenant information, or other ambient values that should
/// not be explicitly passed by callers.
/// </remarks>
public interface IAmbientProvider<TDto>
	: IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously provides ambient values to the specified data transfer object.
	/// </summary>
	/// <param name="dto">The data transfer object to be enriched with ambient values.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task Invoke(TDto dto);
}
