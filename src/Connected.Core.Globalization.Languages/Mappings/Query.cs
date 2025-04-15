using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class Query(ILanguageMappingCache cache)
	: ServiceFunction<IQueryLanguageMappingsDto, IImmutableList<ILanguageMapping>>
{
	protected override async Task<IImmutableList<ILanguageMapping>> OnInvoke()
	{
		return await cache.Where(f => Dto.Language is null || f.Language == Dto.Language).AsEntities<ILanguageMapping>();
	}
}
