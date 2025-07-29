using Connected.Authentication;
using Connected.Identities;
using Connected.Membership.Claims;
using Connected.Membership.Claims.Dtos;
using Connected.Services;

namespace Connected.Membership;
public static class ClaimExtensions
{
	public static async Task<bool> HasFullControl(this IClaimService claims, IIdentity? identity)
	{
		if (identity is null)
			return false;

		var dto = Dto.Factory.Create<IRequestClaimDto>();

		dto.Values = ClaimUtils.FullControl;
		dto.Identity = identity.Token;

		return await claims.Request(dto);
	}

	public static async Task<bool> HasClaim(this IIdentity? identity, IClaimService claims, string claim)
	{
		return await HasClaim(identity, claims, claim, null, null);
	}

	public static async Task<bool> HasClaim(this IIdentity? identity, IClaimService claims, string claim, string? type, string? primaryKey)
	{
		return await ClaimUtils.HasClaim(identity?.Token, claims, claim, type, primaryKey);
	}

	public static async Task<bool> HasClaim(this IAuthenticationService authentication, IClaimService claims, string claim)
	{
		return await HasClaim(authentication, claims, claim, null, null);
	}

	public static async Task<bool> HasClaim(this IAuthenticationService authentication, IClaimService claims, string claim, string? type, string? primaryKey)
	{
		return await (await authentication.SelectIdentity()).HasClaim(claims, claim, type, primaryKey);
	}
}
