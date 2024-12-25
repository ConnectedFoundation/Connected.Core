using Connected.Identities.Authentication;
using Connected.Identities.Globalization;
using Connected.Identities.MetaData;

namespace Connected.Identities;

public static class IdentitiesMetaData
{
	public const string Schema = "identities";

	public const string GlobalizationKey = $"{Schema}.{nameof(IIdentityGlobalization)}";
	public const string MetaDataKey = $"{Schema}.{nameof(IIdentityMetaData)}";
	public const string AuthenticationTokenKey = $"{Schema}.{nameof(IIdentityAuthenticationToken)}";
}