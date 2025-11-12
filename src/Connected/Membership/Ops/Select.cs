using Connected.Services;

namespace Connected.Membership.Ops;

internal class Select(IMembershipCache cache)
  : ServiceFunction<IPrimaryKeyDto<long>, IMembership?>
{
	protected override async Task<IMembership?> OnInvoke()
	{
		return await cache.Get(Dto.Id);
	}
}
