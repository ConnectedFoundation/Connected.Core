using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages;
internal sealed class Select(ILanguageCache cache)
	: ServiceFunction<IPrimaryKeyDto<int>, ILanguage?>
{
	protected override async Task<ILanguage?> OnInvoke()
	{
		return await cache.Where(f => f.Id == Dto.Id).AsEntity();
	}
}
