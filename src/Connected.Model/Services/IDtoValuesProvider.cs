using System.Threading.Tasks;

namespace Connected.Services;
/// <summary>
/// Middleware representing opportunity to set or modify values on the <see cref="IDto"/> objects.
/// </summary>
/// <remarks>
/// Some dtos provide properties that are not mandatory by the caller but must be set before
/// the operation is executed. This Middleware is called before the Validation phase occurs.
/// </remarks>
/// <example>
/// Serial value of the Stock item is not provided by the client but is needed before the goods can be
/// stored in the stock. The platform expects that a process will provide it before the Validation
/// occurs. Depending of the process implementation, it can create a new Serial or use existing one. 
/// </example>
/// <typeparam name="TDto">The type of the arguments to be used by the middleware.</typeparam>
public interface IDtoValuesProvider<TDto> : IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// This method gets called by the platform at the time when the values should be provided.
	/// </summary>
	/// <param name="dto">The arguments instance on which values can be provided.</param>
	Task Invoke(TDto dto);
}
