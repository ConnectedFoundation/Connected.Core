using Connected.Entities;

namespace Connected.Identities.Authentication;

/// <summary>
/// Defines the possible status values for an authentication token.
/// </summary>
/// <remarks>
/// This enumeration represents the operational state of an authentication token,
/// controlling whether the token can be used for authentication purposes.
/// </remarks>
public enum AuthenticationTokenStatus
{
	/// <summary>
	/// The token status has not been set or is undefined.
	/// </summary>
	NotSet = 0,

	/// <summary>
	/// The token is enabled and can be used for authentication.
	/// </summary>
	Enabled = 1,

	/// <summary>
	/// The token is disabled and cannot be used for authentication.
	/// </summary>
	Disabled = 2
}

/// <summary>
/// Represents an identity authentication token entity.
/// </summary>
/// <remarks>
/// This interface defines the contract for authentication tokens used to verify
/// and authenticate user identities. Tokens contain a unique key, optional token value,
/// associated identity, status indicator, and expiration timestamp. All properties
/// use init-only setters to ensure immutability after construction.
/// </remarks>
public interface IIdentityAuthenticationToken
	: IEntity<long>
{
	/// <summary>
	/// Gets the unique key that identifies this authentication token.
	/// </summary>
	string Key { get; init; }

	/// <summary>
	/// Gets the authentication token value.
	/// </summary>
	string? Token { get; init; }

	/// <summary>
	/// Gets the identity identifier associated with this authentication token.
	/// </summary>
	string Identity { get; init; }

	/// <summary>
	/// Gets the operational status of the authentication token.
	/// </summary>
	AuthenticationTokenStatus Status { get; init; }

	/// <summary>
	/// Gets the expiration date and time of the authentication token.
	/// </summary>
	DateTimeOffset? Expire { get; init; }
}