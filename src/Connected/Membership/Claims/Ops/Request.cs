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
		var identities = await MembershipUtils.ResolveIdentityRoles(Dto.Identity, membership, roles);
		var values = Dto.Values.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var targets = await cache.AsEntities(f =>
					values.Any(g => string.Equals(g, f.Value, StringComparison.OrdinalIgnoreCase))
				&& (Dto.Identity == null || identities.Any(g => string.Equals(g, f.Identity, StringComparison.Ordinal)))
				&& (Dto.Entity == null || string.Equals(f.Entity, Dto.Entity, StringComparison.OrdinalIgnoreCase))
				&& (Dto.EntityId == null || string.Equals(f.EntityId, Dto.EntityId, StringComparison.OrdinalIgnoreCase))
				&& f.Status == ClaimStatus.Approved);

		return targets.Any();
	}
}
