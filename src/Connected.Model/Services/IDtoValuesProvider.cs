using System.Threading.Tasks;

namespace Connected.Services;

/// <summary>
/// Middleware representing an opportunity to set or modify values on <see cref="IDto"/> objects.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object for which values are provided.</typeparam>
/// <remarks>
/// Some DTOs provide properties that are not mandatory for the caller but must be set before
/// the operation is executed. This middleware is called before the validation phase occurs,
/// allowing the platform to supply required values automatically. For example, a serial value
/// of a stock item might not be provided by the client but is needed before goods can be
/// stored in stock. The platform expects that a process will provide it before validation
/// occurs. Depending on the process implementation, it can create a new serial or use an
/// existing one.
/// </remarks>
public interface IDtoValuesProvider<TDto>
	: IMiddleware
	where TDto : IDto
{
	/// <summary>
	/// Asynchronously provides or modifies values on the specified data transfer object.
	/// </summary>
	/// <param name="dto">The data transfer object instance on which values can be provided or modified.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task Invoke(TDto dto);
}
