using Connected.Annotations.Entities;
using Connected.Membership.Claims;
using Connected.Membership.Roles;

namespace Connected.Membership;

public static class MembershipMetaData
{
	public const string MembershipKey = $"{SchemaAttribute.CoreSchema}.{nameof(IMembership)}";
	public const string RoleKey = $"{SchemaAttribute.CoreSchema}.{nameof(IRole)}";
	public const string ClaimKey = $"{SchemaAttribute.CoreSchema}.{nameof(IClaim)}";
}
