using System.Threading.Tasks;

namespace Connected.Services;

/// <summary>
/// Represents a service operation that executes logic and returns a value.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object that contains the operation parameters.</typeparam>
/// <typeparam name="TReturnValue">The type of the value returned by the function.</typeparam>
/// <remarks>
/// Functions are service operations that execute business logic and return a result value.
/// Unlike actions, functions provide output to the caller. They extend the service operation
/// contract with state management and caller context capabilities.
/// </remarks>
public interface IFunction<TDto, TReturnValue>
	: IServiceOperation<TDto>
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously invokes the function with the specified data transfer object.
	/// </summary>
	/// <param name="dto">The data transfer object containing the operation parameters.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the function's return value.</returns>
	Task<TReturnValue> Invoke(TDto dto);
}
