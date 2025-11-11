namespace Connected.Authorization.Services;

/// <summary>
/// Defines authorization over a service boundary using a composite DTO.
/// </summary>
public interface IServiceAuthorization
	: IAuthorization
{
	/// <summary>
	/// Performs service-level authorization.
	/// </summary>
	/// <remarks>
	/// Implementations should inspect caller identity and DTO payload to determine whether
	/// the service operation is allowed to execute.
	/// </remarks>
	/// <param name="dto">The service authorization DTO containing call context and payload.</param>
	/// <returns>An authorization result that controls downstream execution.</returns>
	Task<AuthorizationResult> Invoke(IServiceAuthorizationDto dto);
	/*
	 * Contract-only interface; concrete services implement the decision logic.
	 */
}
