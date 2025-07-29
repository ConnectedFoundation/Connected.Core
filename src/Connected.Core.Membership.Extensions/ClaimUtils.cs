using Connected.Membership.Claims;
using Connected.Membership.Claims.Dtos;
using Connected.Services;

namespace Connected.Membership;
public static class ClaimUtils
{
	public const string FullControl = "Full Control";

	public static async Task<bool> HasClaim(string? identity, IClaimService claims, string claim)
	{
		return await HasClaim(identity, claims, claim, null, null);
	}

	public static async Task<bool> HasClaim(string? identity, IClaimService claims, string claim, string? type, string? primaryKey)
	{
		var dto = Dto.Factory.Create<IRequestClaimDto>();

		dto.Identity = identity;
		dto.Values = claim;
		dto.Type = type;
		dto.PrimaryKey = primaryKey;

		return await claims.Request(dto);
	}
}
