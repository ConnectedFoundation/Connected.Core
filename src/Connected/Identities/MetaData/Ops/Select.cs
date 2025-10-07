using Connected.Services;

namespace Connected.Identities.MetaData.Ops;

internal sealed class Select(IIdentityMetaDataCache cache)
  : ServiceFunction<IPrimaryKeyDto<string>, IIdentityMetaData?>
{
	protected override async Task<IIdentityMetaData?> OnInvoke()
	{
		return await cache.Get(Dto.Id);
	}
}
