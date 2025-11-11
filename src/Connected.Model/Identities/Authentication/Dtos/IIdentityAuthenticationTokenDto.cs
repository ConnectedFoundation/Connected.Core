using Connected.Services;

namespace Connected.Identities.Authentication.Dtos;

/// <summary>
/// Represents a data transfer object for identity authentication token information.
/// </summary>
/// <remarks>
/// This interface serves as the base DTO for authentication token operations, containing
/// common properties such as token value, status, and expiration time. It provides the
/// foundational contract for token management across insert, update, and query operations.
/// </remarks>
public interface IIdentityAuthenticationTokenDto
	: IDto
{
	/// <summary>
	/// Gets or sets the authentication token value.
	/// </summary>
	string? Token { get; set; }

	/// <summary>
	/// Gets or sets the status of the authentication token.
	/// </summary>
	AuthenticationTokenStatus Status { get; set; }

	/// <summary>
	/// Gets or sets the expiration date and time of the authentication token.
	/// </summary>
	DateTimeOffset? Expire { get; set; }
}
