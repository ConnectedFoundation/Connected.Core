using Connected.Annotations.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Globalization;
using Connected.Identities.MetaData;

namespace Connected.Identities;

/// <summary>
/// Provides metadata key constants for identity-related entities.
/// </summary>
/// <remarks>
/// This class centralizes entity key identifiers used throughout the identity subsystem
/// for consistent naming and reference resolution. All keys follow the pattern of combining
/// the core schema with the entity interface name.
/// </remarks>
public static class IdentitiesMetaData
{
	/// <summary>
	/// The fully qualified key identifier for the User entity.
	/// </summary>
	public const string UserKey = $"{SchemaAttribute.CoreSchema}.{nameof(IUser)}";

	/// <summary>
	/// The fully qualified key identifier for the IdentityGlobalization entity.
	/// </summary>
	public const string GlobalizationKey = $"{SchemaAttribute.CoreSchema}.{nameof(IIdentityGlobalization)}";

	/// <summary>
	/// The fully qualified key identifier for the IdentityMetaData entity.
	/// </summary>
	public const string MetaDataKey = $"{SchemaAttribute.CoreSchema}.{nameof(IIdentityMetaData)}";

	/// <summary>
	/// The fully qualified key identifier for the IdentityAuthenticationToken entity.
	/// </summary>
	public const string AuthenticationTokenKey = $"{SchemaAttribute.CoreSchema}.{nameof(IIdentityAuthenticationToken)}";
}