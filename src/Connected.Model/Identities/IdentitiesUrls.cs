namespace Connected.Identities;

public static class IdentitiesUrls
{
	public const string Identities = "services/identities";

	public const string Users = $"{Identities}/users";
	public const string UserManagement = $"{Identities}/user-management";
	public const string GlobalizationService = $"{Identities}/globalization";
	public const string MetaDataService = $"{Identities}/meta-data";
	public const string AuthenticationTokenService = $"{Identities}/authentication-tokens";

	public const string SelectByCredentialsOperation = "select-by-credentials";
	public const string ResolveOperation = "resolve";
	public const string LookupOperation = "lookup";
	public const string LookupByTokenOperation = "lookup-by-token";
	public const string UpdatePasswordOperation = "update-password";
}