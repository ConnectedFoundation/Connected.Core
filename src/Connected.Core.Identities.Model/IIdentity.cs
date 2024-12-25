namespace Connected.Identities;
/// <summary>
/// Represents an Identity which acts as an Authenticated entity in the specified context.
/// </summary>
/// <remarks>
/// Identity can be eny entity that can provide a security token which is the only property
/// this interface provides.
/// </remarks>
public interface IIdentity
{
	/// <summary>
	/// A Security token which uniquely identifies an Entity in the Authentication system.
	/// </summary>
	string Token { get; init; }
}