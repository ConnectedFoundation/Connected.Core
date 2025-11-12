using Connected.Identities;

namespace Connected.Authentication;
/// <summary>
/// Represents a privileged system identity used for elevated operations in the
/// authentication pipeline. This identity carries a well-known static token.
/// </summary>
internal sealed class SystemIdentity
	: IIdentity
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SystemIdentity"/> with a predefined token.
	/// </summary>
	public SystemIdentity()
	{
		/*
		 * Assign a constant token value that uniquely represents the system identity.
		 * This token can be used to bypass standard user checks where system-level
		 * privileges are required (e.g., performing internal service operations).
		 */
		Token = "9A328F3D-C599-48FF-BE35-629C7673EE82";
	}
	/// <summary>
	/// Gets the security token that uniquely identifies this system identity.
	/// </summary>
	public string Token { get; init; }
}
