using Connected.Annotations.Entities;
using Connected.Identities.Authentication;
using Connected.Identities.Globalization;
using Connected.Identities.MetaData;

namespace Connected.Identities;

public static class IdentitiesMetaData
{
	public const string UserKey = $"{SchemaAttribute.CoreSchema}.{nameof(IUser)}";
	public const string GlobalizationKey = $"{SchemaAttribute.CoreSchema}.{nameof(IIdentityGlobalization)}";
	public const string MetaDataKey = $"{SchemaAttribute.CoreSchema}.{nameof(IIdentityMetaData)}";
	public const string AuthenticationTokenKey = $"{SchemaAttribute.CoreSchema}.{nameof(IIdentityAuthenticationToken)}";
}