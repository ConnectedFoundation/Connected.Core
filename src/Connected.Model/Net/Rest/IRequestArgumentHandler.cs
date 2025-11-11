using Connected.Net.Rest.Dtos;

namespace Connected.Net.Rest;

/// <summary>
/// Defines a middleware component for handling REST request arguments.
/// </summary>
/// <remarks>
/// This interface represents specialized middleware that processes individual request
/// arguments from REST API calls. Request argument handlers can perform operations such
/// as validation, transformation, enrichment, or custom processing logic on specific
/// request parameters. Multiple handlers can be registered to process different arguments,
/// enabling a pipeline-based approach to argument handling. As middleware components,
/// request argument handlers participate in the application's lifecycle and can be
/// dynamically discovered and initialized. This pattern is useful for cross-cutting
/// concerns that apply to specific request parameters across multiple API operations.
/// </remarks>
public interface IRequestArgumentHandler
	: IMiddleware
{
	/// <summary>
	/// Asynchronously invokes the handler to process a request argument.
	/// </summary>
	/// <param name="dto">The request argument containing the property name and value to process.</param>
	/// <returns>A task that represents the asynchronous argument processing operation.</returns>
	/// <remarks>
	/// This method is called when a matching request argument needs to be processed.
	/// Implementations should examine the argument's property name and value, then perform
	/// appropriate processing such as validation, transformation, or side effects. Handlers
	/// can modify the argument value if needed or throw exceptions for validation failures.
	/// The asynchronous nature allows handlers to perform I/O operations such as lookups
	/// or external validations.
	/// </remarks>
	Task Invoke(IRequestArgumentDto dto);
}
