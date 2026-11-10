namespace Connected.Identities.Authentication.Dtos;

/// <summary>
/// Represents a data transfer object for inserting a new identity authentication token.
/// </summary>
/// <remarks>
/// This interface extends the base authentication token DTO with additional properties
/// required for token creation, including the identity identifier and a unique key
/// associated with the token.
/// </remarks>
public interface IInsertIdentityAuthenticationTokenDto
	: IIdentityAuthenticationTokenDto
{
	/// <summary>
	/// Gets or sets the identity identifier for which the authentication token is created.
	/// </summary>
	string Identity { get; set; }

	/// <summary>
	/// Gets or sets the unique key associated with the authentication token.
	/// </summary>
	string Key { get; set; }

	/// <summary>
	/// Gets or sets the creation time of the token.
	/// </summary>
	/// <remarks>
	/// Leave unset for a new token; the current time is used. Set it only when a token is re-issued and
	/// must keep the creation time, and therefore the maximum lifetime, of the token it replaces. The default
	/// implementation keeps existing implementations of this interface valid.
	/// </remarks>
	DateTimeOffset? Created { get => null; set { } }
}