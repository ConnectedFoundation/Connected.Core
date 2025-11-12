namespace Connected.Services.Middlewares;

/// <summary>
/// Represents middleware for intercepting and processing service function operations.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object that contains the function parameters.</typeparam>
/// <typeparam name="TReturnValue">The type of the value returned by the function.</typeparam>
/// <remarks>
/// This middleware interface enables interception of function operations before or after their execution,
/// allowing cross-cutting concerns such as caching, result transformation, logging, or error handling
/// to be applied consistently across service functions. The middleware receives both the function context
/// and its result, enabling inspection and modification of the return value.
/// </remarks>
public interface IServiceFunctionMiddleware<TDto, TReturnValue>
	: IServiceOperationMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously invokes the middleware to process the service function and its result.
	/// </summary>
	/// <param name="operation">The function operation containing the data transfer object and execution context.</param>
	/// <param name="result">The result returned by the function, or null if no result was produced.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the potentially modified function result.</returns>
	Task<TReturnValue?> Invoke(IFunction<TDto, TReturnValue> operation, TReturnValue? result);
}
