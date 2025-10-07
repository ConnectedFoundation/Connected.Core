using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages;
internal sealed class Query(ILanguageCache cache)
	: ServiceFunction<IQueryDto, IImmutableList<ILanguage>>
{
	protected override async Task<IImmutableList<ILanguage>> OnInvoke()
	{
		return await cache.WithDto(Dto).AsEntities<ILanguage>();
	}
}
