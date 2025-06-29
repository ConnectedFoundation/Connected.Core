using Connected.Entities;
using Connected.Membership.Claims.Dtos;
using Connected.Membership.Roles;
using Connected.Services;

namespace Connected.Membership.Claims.Ops;

internal class Request(IClaimCache cache, IMembershipService membership, IRoleService roles)
  : ServiceFunction<IRequestClaimDto, bool>
{
	protected override async Task<bool> OnInvoke()
	{
		var identities = await MembershipUtils.ResolveIdentityTokens(Dto.Identity, membership, roles);
		var values = Dto.Values.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var targets = await cache.AsEntities<IClaim>(f =>
					values.Any(g => string.Equals(g, f.Value, StringComparison.OrdinalIgnoreCase))
				&& (Dto.Identity is null || identities.Any(g => string.Equals(g, f.Identity, StringComparison.Ordinal)))
				&& (Dto.Type is null || string.Equals(f.Type, Dto.Type, StringComparison.OrdinalIgnoreCase))
				&& (Dto.PrimaryKey is null || string.Equals(f.PrimaryKey, Dto.PrimaryKey, StringComparison.OrdinalIgnoreCase)));

		return targets.Any();
	}
}
