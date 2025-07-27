namespace Connected.Services;

/// <summary>
/// Middleware for correcting <see cref="IDto"/> objects and <see cref="IAmbientProvider{TDto}"/>  objects before
/// they are validated.
/// </summary>
/// <remarks>
/// When this stage occurs, all DtoValueProviders and Ambients providers were already executed and it is guaranteed
/// that they do not interfere when those middlewares are executed.
/// </remarks>
/// <typeparam name="TDto">The type of the arguments to be used by the middleware.</typeparam>
public interface ICalibrator<TDto> : IMiddleware
	where TDto : IDto
{
	Task Invoke(TDto dto);
}