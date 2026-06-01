using Connected.Caching;
using Connected.Entities;
using Connected.Globalization.Languages.Mappings.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages.Mappings.Ops;

internal sealed class Query(IEntityCache<ILanguageMapping, int> cache)
	: ServiceFunction<IQueryLanguageMappingDto, IImmutableList<ILanguageMapping>>
{
	protected override async Task<IImmutableList<ILanguageMapping>> OnInvoke()
	{
		return await cache.Where(f => Dto.Language == null || f.Language == Dto.Language).AsEntities<ILanguageMapping>();
	}
}
