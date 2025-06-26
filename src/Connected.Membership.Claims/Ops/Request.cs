using Connected.Entities;
using Connected.Membership.Claims.Dtos;
using Connected.Services;

namespace Connected.Membership.Claims.Ops;

internal class Request(IClaimCache cache)
  : ServiceFunction<IRequestClaimDto, bool>
{
	protected override async Task<bool> OnInvoke()
	{
		var values = Dto.Values.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var targets = await cache.AsEntities<IClaim>(f =>
					values.Any(g => string.Equals(g, f.Value, StringComparison.OrdinalIgnoreCase))
				&& (Dto.Identity is null || string.Equals(f.Identity, Dto.Identity, StringComparison.Ordinal))
				&& (Dto.Type is null || string.Equals(f.Type, Dto.Type, StringComparison.OrdinalIgnoreCase))
				&& (Dto.PrimaryKey is null || string.Equals(f.PrimaryKey, Dto.PrimaryKey, StringComparison.OrdinalIgnoreCase)));

		return targets.Any();
	}
}
