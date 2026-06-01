using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages.Ops;

internal sealed class Select(IEntityCache<ILanguage, int> cache)
	: ServiceFunction<IPrimaryKeyDto<int>, ILanguage?>
{
	protected override async Task<ILanguage?> OnInvoke()
	{
		return await cache.Where(f => f.Id == Dto.Id).AsEntity();
	}
}
