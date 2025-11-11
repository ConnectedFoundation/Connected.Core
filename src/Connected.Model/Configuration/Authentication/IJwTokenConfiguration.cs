namespace Connected.Configuration.Authentication;
/// <summary>
/// Configuration contract describing JSON Web Token (JWT) issuance and validation parameters.
/// </summary>
/// <remarks>
/// Provides values used to generate and validate JWTs: issuer and audience strings, the signing key,
/// and token duration (in minutes or seconds depending on consumer implementation). Consumers map these
/// properties to security token descriptors during creation and to validation parameters when parsing
/// incoming tokens.
/// </remarks>
public interface IJwTokenConfiguration
{
	/// <summary>
	/// Gets the expected token issuer.
	/// </summary>
	string? Issuer { get; }
	/// <summary>
	/// Gets the expected token audience.
	/// </summary>
	string? Audience { get; }
	/// <summary>
	/// Gets the symmetric signing key (or key identifier) used for token generation/validation.
	/// </summary>
	string? Key { get; }
	/// <summary>
	/// Gets the duration value controlling token lifetime.
	/// </summary>
	int Duration { get; }
}