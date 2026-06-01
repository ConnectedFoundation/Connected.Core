using Connected.Caching;
using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages.Dtos;

internal sealed class SelectByLcid(IEntityCache<ILanguage, int> cache)
	: ServiceFunction<ISelectLanguageDto, ILanguage?>
{
	protected override async Task<ILanguage?> OnInvoke()
	{
		return await cache.Where(f => f.Lcid == Dto.Lcid).AsEntity();
	}
}
