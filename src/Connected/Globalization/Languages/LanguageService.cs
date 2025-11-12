using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages;
internal sealed class LanguageService(IServiceProvider services) : Service(services), ILanguageService
{
	public async Task Delete(IPrimaryKeyDto<int> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<int> Insert(IInsertLanguageDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<ILanguage>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.NoPaging);
	}

	public async Task<ILanguage?> Select(IPrimaryKeyDto<int> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task<ILanguage?> Select(ISelectLanguageDto dto)
	{
		return await Invoke(GetOperation<SelectByLcid>(), dto);
	}

	public async Task Update(IUpdateLanguageDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}
}
