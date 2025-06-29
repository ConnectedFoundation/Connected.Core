namespace Connected.Membership;

public static class MembershipUrls
{
	public const string Namespace = "services/membership";

	public const string RoleService = $"{Namespace}/roles";
	public const string MembershipService = $"{Namespace}/membership";
	public const string ClaimService = $"{Namespace}/claims";
	public const string ClaimSchemaService = $"{Namespace}/claims/schema";

	public const string SelectByNameOperation = "select-by-name";
	public const string QueryClaimsOperation = "query-claims";
}