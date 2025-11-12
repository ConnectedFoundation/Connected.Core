using Connected.Services;

namespace Connected.Identities.MetaData.Dtos;

/// <summary>
/// Represents a base data transfer object for identity metadata information.
/// </summary>
/// <remarks>
/// This interface defines the common metadata properties associated with an identity,
/// including profile information such as URL, description, avatar, and username.
/// It serves as the foundational contract for identity metadata operations across
/// insert and update scenarios.
/// </remarks>
public interface IIdentityMetaDataDto
	: IPrimaryKeyDto<string>
{
	/// <summary>
	/// Gets or sets the URL associated with the identity.
	/// </summary>
	string? Url { get; set; }

	/// <summary>
	/// Gets or sets the description or bio of the identity.
	/// </summary>
	string? Description { get; set; }

	/// <summary>
	/// Gets or sets the avatar image URL or identifier for the identity.
	/// </summary>
	string? Avatar { get; set; }

	/// <summary>
	/// Gets or sets the username for the identity.
	/// </summary>
	string? UserName { get; set; }
}
