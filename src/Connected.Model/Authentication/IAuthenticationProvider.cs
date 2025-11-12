namespace Connected.Authentication;
/// <summary>
/// An interface for implementing authentication providers.
/// </summary>
public interface IAuthenticationProvider : IMiddleware
{
	/*
	 * Providers implement a single entry point invoked by the authentication pipeline.
	 * The supplied DTO carries scheme and token values used to resolve and set identity.
	 */
	/// <summary>
	/// Called from the authentication middleware to evaluate credentials and establish identity
	/// for the current scope.
	/// </summary>
	/// <param name="dto">Authentication data including scheme and token.</param>
	Task Invoke(IAuthenticateDto dto);
}