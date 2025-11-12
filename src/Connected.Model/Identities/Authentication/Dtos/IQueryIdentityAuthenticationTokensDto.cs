using Connected.Services;

namespace Connected.Identities.Authentication.Dtos;

/// <summary>
/// Represents a data transfer object for querying identity authentication tokens.
/// </summary>
/// <remarks>
/// This interface provides filter criteria for querying authentication tokens, allowing
/// searches by identity identifier and token key. Both properties are optional to support
/// flexible query scenarios.
/// </remarks>
public interface IQueryIdentityAuthenticationTokensDto
	: IDto
{
	/// <summary>
	/// Gets or sets the identity identifier to filter authentication tokens.
	/// </summary>
	string? Identity { get; set; }

	/// <summary>
	/// Gets or sets the token key to filter authentication tokens.
	/// </summary>
	string? Key { get; set; }
}