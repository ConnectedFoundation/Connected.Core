using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Authentication.Ops;

internal class Select(IStorageProvider storage, IIdentityAuthenticationTokenCache cache)
  : ServiceFunction<IPrimaryKeyDto<long>, IIdentityAuthenticationToken?>
{
	protected override async Task<IIdentityAuthenticationToken?> OnInvoke()
	{
		return await cache.Get(Dto.Id, async (f) =>
		{
			return await storage.Open<IdentityAuthenticationToken>().AsEntity(f => f.Id == Dto.Id);
		});
	}
}
