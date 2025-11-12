namespace Connected.Services;

/// <summary>
/// Represents the base interface for all service operations.
/// </summary>
/// <typeparam name="TDto">The type of the data transfer object that contains the operation parameters.</typeparam>
/// <remarks>
/// This interface provides the foundational contract for service operations, including
/// access to the operation's DTO, caller context, and state management capabilities.
/// Both actions and functions extend this interface to implement specific operation types.
/// </remarks>
public interface IServiceOperation<TDto>
	: IOperationState
	where TDto : IDto
{
	/// <summary>
	/// Gets the data transfer object containing the operation parameters.
	/// </summary>
	TDto Dto { get; }

	/// <summary>
	/// Gets the caller context providing information about the operation's initiator.
	/// </summary>
	ICallerContext Caller { get; }
}
