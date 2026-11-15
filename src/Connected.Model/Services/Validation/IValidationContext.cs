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

	/// <summary>
	/// Tells the context that <paramref name="value"/> was bound from the caller's request, so validating it also holds it
	/// to its <c>required</c> members.
	/// </summary>
	/// <remarks>
	/// <para>
	/// C# <c>required</c> is enforced by the compiler only. A binder fills a request's DTO without it, so a member the
	/// caller left out arrives null. A DTO given here, and the DTOs nested in it, are refused when such a member whose type
	/// does not allow null is null, naming the member. A DTO built by code is never given here and is not held to it, so
	/// no internal call is refused for what it leaves unset.
	/// </para>
	/// <para>
	/// The context is scoped: what it was given lives as long as the request's scope and is not seen by other requests.
	/// </para>
	/// </remarks>
	/// <param name="value">The DTO a binder filled from the caller's request.</param>
	void Bound(IDto value);
}
