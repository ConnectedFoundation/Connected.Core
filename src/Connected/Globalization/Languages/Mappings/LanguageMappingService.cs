using Connected.Globalization.Languages.Mappings.Dtos;
using Connected.Globalization.Languages.Mappings.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages.Mappings;

internal sealed class LanguageMappingService(IServiceProvider services) : Service(services), ILanguageMappingService
{
	public async Task<IImmutableList<ILanguageMapping>> Query(IQueryLanguageMappingDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task<ILanguageMapping?> Select(IPrimaryKeyDto<int> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}
}
