using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages;
internal sealed class Query(ILanguageCache cache)
	: ServiceFunction<IQueryDto, ImmutableList<ILanguage>>
{
	protected override async Task<ImmutableList<ILanguage>> OnInvoke()
	{
		return await cache.WithDto(Dto).AsEntities<ILanguage>();
	}
}
