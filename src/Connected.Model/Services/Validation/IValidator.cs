namespace Connected.Services.Validation;

/// <summary>
/// Represents a validator middleware for data transfer objects.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object to validate.</typeparam>
/// <remarks>
/// This middleware interface enables validation of specific DTO types before they are
/// processed by service operations. Validators execute during the validation phase of
/// the service operation pipeline, allowing custom validation rules to be applied based
/// on business logic requirements. Multiple validators can be registered for a single
/// DTO type, and they will execute in registration order.
/// </remarks>
public interface IValidator<TDto>
	: IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously validates the specified data transfer object.
	/// </summary>
	/// <param name="caller">The caller context providing information about the operation's initiator.</param>
	/// <param name="dto">The data transfer object to be validated.</param>
	/// <returns>A task that represents the asynchronous validation operation.</returns>
	Task Invoke(ICallerContext caller, TDto dto);
}
