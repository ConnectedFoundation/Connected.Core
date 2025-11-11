namespace Connected.Authorization.Services;
/// <summary>
/// Defines an authorization component that evaluates the current scope context and returns
/// an <see cref="AuthorizationResult"/> governing further processing.
/// </summary>
public interface IScopeAuthorization
	: IAuthorization
{
	/// <summary>
	/// Performs authorization for the supplied scope DTO.
	/// </summary>
	/// <remarks>
	/// Implementations inspect caller context and operation DTO within the scope to determine
	/// whether to pass, fail, or skip authorization for downstream handlers.
	/// </remarks>
	/// <param name="dto">Composite DTO containing caller and operation data.</param>
	/// <returns>Authorization decision result.</returns>
	Task<AuthorizationResult> Invoke(IScopeAuthorizationDto dto);
	/*
	 * Method contract only; concrete implementations analyze dto contents to compute result.
	 */
}
