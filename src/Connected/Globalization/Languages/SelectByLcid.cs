using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages;
internal sealed class SelectByLcid(ILanguageCache cache)
	: ServiceFunction<ISelectLanguageDto, ILanguage?>
{
	protected override async Task<ILanguage?> OnInvoke()
	{
		return await cache.Where(f => f.Lcid == Dto.Lcid).AsEntity();
	}
}
