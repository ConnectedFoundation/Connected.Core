using Connected.Identities;
using Connected.Services;

namespace Connected.Authentication;
/// <summary>
/// Data transfer object used to update the current identity within the authentication system.
/// </summary>
public interface IUpdateIdentityDto : IDto
{
	/*
	 * Holds the new identity instance to apply for the current scope. Providers
	 * and services can set this to switch the ambient principal.
	 */
	/// <summary>
	/// Gets or sets the identity to be applied to the current scope.
	/// </summary>
	IIdentity? Identity { get; set; }
}
