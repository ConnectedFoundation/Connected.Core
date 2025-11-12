using Connected.Entities;
using Connected.Services;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class Select(ILanguageMappingCache cache)
	: ServiceFunction<IPrimaryKeyDto<int>, ILanguageMapping?>
{
	protected override async Task<ILanguageMapping?> OnInvoke()
	{
		return await cache.Where(f => f.Id == Dto.Id).AsEntity();
	}
}
