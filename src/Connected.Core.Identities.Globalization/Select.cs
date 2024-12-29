using Connected.Entities;
using Connected.Services;

namespace Connected.Identities.Globalization;

internal sealed class Select(IIdentityGlobalizationCache cache) : ServiceFunction<IPrimaryKeyDto<string>, IIdentityGlobalization?>
{
	protected override async Task<IIdentityGlobalization?> OnInvoke()
	{
		return await cache.AsEntity(f => string.Equals(f.Id, Dto.Id, StringComparison.Ordinal));
	}
}