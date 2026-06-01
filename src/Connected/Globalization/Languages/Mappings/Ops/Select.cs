using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages.Mappings.Ops;

internal sealed class Select(IEntityCache<ILanguageMapping, int> cache)
	: ServiceFunction<IPrimaryKeyDto<int>, ILanguageMapping?>
{
	protected override async Task<ILanguageMapping?> OnInvoke()
	{
		return await cache.Where(f => f.Id == Dto.Id).AsEntity();
	}
}
