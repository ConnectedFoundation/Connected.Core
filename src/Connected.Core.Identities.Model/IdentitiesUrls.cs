namespace Connected.Identities;

public static class IdentitiesUrls
{
	private const string IdentityServices = "services/identities";

	public const string Users = $"{IdentityServices}/users";
	public const string UserManagement = $"{IdentityServices}/user-management";
	public const string GlobalizationService = $"{IdentityServices}/globalization";
	public const string MetaDataService = $"{IdentityServices}/meta-data";
	public const string AuthenticationTokenService = $"{IdentityServices}/authentication-tokens";

	public const string SelectByCredentialsOperation = "select-by-redentials";
	public const string ResolveOperation = "resolve";
	public const string LookupOperation = "lookup";
}