namespace Connected.Services;

/// <summary>
/// Represents a service operation that performs an action without returning a value.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object that contains the operation parameters.</typeparam>
/// <remarks>
/// Actions are service operations that execute business logic and modify state but do not
/// return a result value. They extend the service operation contract with state management
/// and caller context capabilities.
/// </remarks>
public interface IAction<TDto>
	: IServiceOperation<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously invokes the action with the specified data transfer object.
	/// </summary>
	/// <param name="dto">The data transfer object containing the operation parameters.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task Invoke(TDto dto);
}
