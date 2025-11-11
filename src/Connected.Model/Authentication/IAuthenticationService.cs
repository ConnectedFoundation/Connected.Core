using Connected.Annotations;
using Connected.Identities;

namespace Connected.Authentication;
/// <summary>
/// Provides authentication information for the current scope.
/// </summary>
[Service]
public interface IAuthenticationService
{
	/*
	 * Exposes operations to retrieve or update the ambient identity tied to the current execution scope.
	 * The select method returns null for anonymous requests; update applies a new identity instance.
	 */
	/// <summary>
	/// Gets the identity for the current scope. Returns null when the scope is anonymous.
	/// </summary>
	/// <returns>The current identity or null.</returns>
	Task<IIdentity?> SelectIdentity();
	/// <summary>
	/// Updates the current scope's identity using the supplied DTO.
	/// </summary>
	/// <param name="dto">DTO containing new identity information.</param>
	Task UpdateIdentity(IUpdateIdentityDto dto);
}