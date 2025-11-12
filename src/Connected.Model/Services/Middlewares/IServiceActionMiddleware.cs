namespace Connected.Services.Middlewares;

/// <summary>
/// Represents middleware for intercepting and processing service action operations.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object that contains the action parameters.</typeparam>
/// <remarks>
/// This middleware interface enables interception of action operations before or after their execution,
/// allowing cross-cutting concerns such as logging, validation, authorization, or transaction management
/// to be applied consistently across service actions. The middleware receives the complete action context
/// including the operation state and caller information.
/// </remarks>
public interface IServiceActionMiddleware<TDto>
	: IServiceOperationMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously invokes the middleware to process the service action.
	/// </summary>
	/// <param name="dto">The action operation containing the data transfer object and execution context.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task Invoke(IAction<TDto> dto);
}