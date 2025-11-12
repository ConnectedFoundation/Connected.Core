namespace Connected.Services.Validation;

/// <summary>
/// Provides validation context for data transfer objects.
/// </summary>
/// <remarks>
/// This interface enables validation of DTOs before they are processed by service operations.
/// It provides a centralized mechanism for executing validation logic based on the caller
/// context and the DTO being validated. Validation failures typically result in exceptions
/// that halt operation execution.
/// </remarks>
public interface IValidationContext
{
	/// <summary>
	/// Asynchronously validates the specified data transfer object.
	/// </summary>
	/// <typeparam name="TDto">The type of the data transfer object to validate.</typeparam>
	/// <param name="caller">The caller context providing information about the operation's initiator.</param>
	/// <param name="value">The data transfer object to be validated.</param>
	/// <returns>A task that represents the asynchronous validation operation.</returns>
	Task Validate<TDto>(ICallerContext caller, TDto value) where TDto : IDto;
}
