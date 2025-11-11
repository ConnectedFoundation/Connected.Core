using System.Security.Claims;
using System.Security.Principal;

namespace Connected.Authentication;
/// <summary>
/// Default implementation of a <see cref="ClaimsPrincipal"/> that exposes a provided
/// <see cref="System.Security.Principal.IIdentity"/> instance via the overridden
/// <see cref="Identity"/> property.
/// </summary>
/// <param name="identity">The identity to associate with this principal.</param>
public class DefaultPrincipal(IIdentity? identity)
	: ClaimsPrincipal
{
	/// <summary>
	/// Gets the identity associated with this principal instance.
	/// </summary>
	public override IIdentity? Identity { get; } = identity;
	/*
	 * The principal simply wraps the supplied identity and surfaces it through the
	 * standard Identity property, enabling compatibility with authentication flows
	 * that expect a ClaimsPrincipal-derived type.
	 */
}