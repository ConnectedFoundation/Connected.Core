namespace Connected.Authentication;
/// <summary>
/// An interface for implementing authentication providers.
/// </summary>
public interface IAuthenticationProvider : IMiddleware
{
	/// <summary>
	/// This method is called from the appropriate middleware 
	/// to determine the identity for the specified scope.
	/// </summary>
	Task Invoke(IAuthenticateDto dto);
}