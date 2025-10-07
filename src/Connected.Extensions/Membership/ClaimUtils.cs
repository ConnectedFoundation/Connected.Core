using Connected.Membership.Claims;
using Connected.Membership.Claims.Dtos;
using Connected.Services;

namespace Connected.Membership;
public static class ClaimUtils
{
	public const string FullControl = "Full Control";

	public static async Task<bool> HasClaim(string? identity, IClaimService claims, string claim, string entity, string entityId)
	{
		var dto = Dto.Factory.Create<IRequestClaimDto>();

		dto.Identity = identity;
		dto.Values = claim;
		dto.Entity = entity;
		dto.EntityId = entityId;

		return await claims.Request(dto);
	}
}
