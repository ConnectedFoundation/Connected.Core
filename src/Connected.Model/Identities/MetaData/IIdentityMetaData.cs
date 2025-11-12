using Connected.Entities;

namespace Connected.Identities.MetaData;

/// <summary>
/// Represents an identity metadata entity containing profile and descriptive information.
/// </summary>
/// <remarks>
/// This interface defines the contract for storing metadata associated with an identity,
/// including profile information such as URL, description, avatar, and username. The entity
/// uses a string-based primary key and init-only properties to ensure immutability after
/// construction. These metadata attributes enhance user profiles with additional descriptive
/// and visual elements.
/// </remarks>
public interface IIdentityMetaData
	: IEntity<string>
{
	/// <summary>
	/// Gets the URL associated with the identity.
	/// </summary>
	string? Url { get; init; }

	/// <summary>
	/// Gets the description or bio of the identity.
	/// </summary>
	string? Description { get; init; }

	/// <summary>
	/// Gets the avatar image URL or identifier for the identity.
	/// </summary>
	string? Avatar { get; init; }

	/// <summary>
	/// Gets the username for the identity.
	/// </summary>
	string? UserName { get; init; }
}