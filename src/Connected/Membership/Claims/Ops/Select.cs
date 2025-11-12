using Connected.Services;

namespace Connected.Membership.Claims.Ops;

internal class Select(IClaimCache cache)
  : ServiceFunction<IPrimaryKeyDto<long>, IClaim?>
{
	protected override async Task<IClaim?> OnInvoke()
	{
		return await cache.Get(Dto.Id);
	}
}
