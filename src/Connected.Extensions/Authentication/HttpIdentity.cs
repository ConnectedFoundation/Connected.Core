using Connected.Identities;

namespace Connected.Authentication;
/// <summary>
/// Represents an HTTP-centric identity which implements <see cref="System.Security.Principal.IIdentity"/>
/// and exposes the domain <see cref="Connected.Identities.IIdentity"/> through <see cref="Identity"/>.
/// </summary>
/// <param name="identity">The domain identity instance associated with this HTTP identity.</param>
public sealed class HttpIdentity(IIdentity? identity)
	: System.Security.Principal.IIdentity, IIdentityAccessor
{
	/// <summary>
	/// Gets the authentication type string advertised for this identity.
	/// </summary>
	/// <remarks>
	/// The HTTP identity bridges between the framework's IIdentity (used by hosting/auth
	/// middleware) and the domain-level IIdentity surfaced via IIdentityAccessor.It pins
	/// AuthenticationType to a constant value and allows callers to set Name and
	/// IsAuthenticated when constructing or initializing the instance.
	/// </remarks>
	public string? AuthenticationType { get; } = "Connected";
	/// <summary>
	/// Gets a value indicating whether the identity has been authenticated.
	/// </summary>
	public bool IsAuthenticated { get; init; }
	/// <summary>
	/// Gets the user name associated with this identity.
	/// </summary>
	public string? Name { get; init; }
	/// <summary>
	/// Gets the domain identity instance associated with this HTTP identity.
	/// </summary>
	public IIdentity? Identity { get; } = identity;
}
