using Connected.Annotations;
using Connected.Identities;

namespace Connected.Authentication;
/// <summary>
/// Provides authentication information for the current scope.
/// </summary>
[Service]
public interface IAuthenticationService
{
	/// <summary>
	/// Gets the identity for the current scope. This property is null is the scope is anonymous.
	/// </summary>
	Task<IIdentity?> SelectIdentity();
	Task UpdateIdentity(IUpdateIdentityDto identity);
}