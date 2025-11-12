namespace Connected.Identities;

/// <summary>
/// Provides URL constants for identity service endpoints and operations.
/// </summary>
/// <remarks>
/// This class centralizes all service endpoint paths and operation names for the identity
/// module, ensuring consistent routing across the application for user management, globalization,
/// metadata, and authentication token services.
/// </remarks>
public static class IdentitiesUrls
{
	/// <summary>
	/// The base service path for identity-related operations.
	/// </summary>
	public const string Identities = "services/identities";

	/// <summary>
	/// The service endpoint URL for user operations.
	/// </summary>
	public const string Users = $"{Identities}/users";

	/// <summary>
	/// The service endpoint URL for user management operations.
	/// </summary>
	public const string UserManagement = $"{Identities}/user-management";

	/// <summary>
	/// The service endpoint URL for identity globalization operations.
	/// </summary>
	public const string GlobalizationService = $"{Identities}/globalization";

	/// <summary>
	/// The service endpoint URL for identity metadata operations.
	/// </summary>
	public const string MetaDataService = $"{Identities}/meta-data";

	/// <summary>
	/// The service endpoint URL for authentication token operations.
	/// </summary>
	public const string AuthenticationTokenService = $"{Identities}/authentication-tokens";

	/// <summary>
	/// The operation name for selecting a user by credentials.
	/// </summary>
	public const string SelectByCredentialsOperation = "select-by-credentials";

	/// <summary>
	/// The operation name for resolving an identity.
	/// </summary>
	public const string ResolveOperation = "resolve";

	/// <summary>
	/// The operation name for looking up identities.
	/// </summary>
	public const string LookupOperation = "lookup";

	/// <summary>
	/// The operation name for looking up identities by token.
	/// </summary>
	public const string LookupByTokenOperation = "lookup-by-token";

	/// <summary>
	/// The operation name for updating a user's password.
	/// </summary>
	public const string UpdatePasswordOperation = "update-password";
}