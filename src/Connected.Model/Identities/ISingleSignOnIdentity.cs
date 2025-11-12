namespace Connected.Identities;

/// <summary>
/// Represents an identity that supports single sign-on authentication.
/// </summary>
/// <remarks>
/// This interface provides a contract for identities that can be authenticated through
/// single sign-on mechanisms. The authentication token is used to validate and establish
/// the identity's session across multiple systems or services.
/// </remarks>
public interface ISingleSignOnIdentity
{
	/// <summary>
	/// Gets the authentication token used for single sign-on verification.
	/// </summary>
	string? AuthenticationToken { get; init; }
}