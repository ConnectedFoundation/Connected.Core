using Connected.Identities;
using Connected.Membership.Claims;
using Connected.Membership.Claims.Dtos;
using Connected.Services;
using Microsoft.Extensions.DependencyInjection;

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

	public static async Task<bool> HasManageSystemSecurity(this IClaimService claims, IIdentity? identity)
	{
		if (identity is null)
			return false;

		if (await claims.HasFullControl(identity))
			return true;

		var dto = Dto.Factory.Create<IRequestClaimDto>();

		dto.Values = ClaimUtils.ManageSystemSecurity;
		dto.Identity = identity.Token;

		return await claims.Request(dto);
	}

	public static async Task<bool> HasClaim(this IIdentity identity, string claim)
	{
		using var scope = ServiceExtensionsStartup.Services.CreateAsyncScope();

		var claims = scope.ServiceProvider.GetRequiredService<IClaimService>();
		var dto = scope.ServiceProvider.GetRequiredService<IRequestClaimDto>();

		dto.Identity = identity.Token;
		dto.Values = claim;

		var result = await claims.Request(dto);

		await scope.Commit();

		return result;
	}
}
