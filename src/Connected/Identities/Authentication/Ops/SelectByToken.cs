using Connected.Entities;
using Connected.Services;
using Connected.Storage;

namespace Connected.Identities.Authentication.Ops;

internal class SelectByToken(IStorageProvider storage, IIdentityAuthenticationTokenCache cache)
  : ServiceFunction<IValueDto<string>, IIdentityAuthenticationToken?>
{
	protected override async Task<IIdentityAuthenticationToken?> OnInvoke()
	{
		return await cache.Get(f => string.Equals(f.Token, Dto.Value, StringComparison.Ordinal), async (f) =>
		{
			return await storage.Open<IdentityAuthenticationToken>().AsEntity(f => string.Equals(f.Token, Dto.Value, StringComparison.Ordinal));
		});
	}
}
