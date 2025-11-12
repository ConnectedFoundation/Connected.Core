namespace Connected.Identities;

/// <summary>
/// Represents an Identity which acts as an Authenticated entity in the specified context.
/// </summary>
/// <remarks>
/// Identity can be any entity that can provide a security token which is the only property
/// this interface provides. The token uniquely identifies the entity in the authentication system.
/// </remarks>
public interface IIdentity
{
	/// <summary>
	/// Gets the security token which uniquely identifies an entity in the authentication system.
	/// </summary>
	string Token { get; init; }
}