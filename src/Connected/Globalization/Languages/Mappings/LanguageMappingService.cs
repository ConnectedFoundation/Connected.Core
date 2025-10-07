using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Globalization.Languages.Mappings;
internal sealed class LanguageMappingService(IServiceProvider services) : Service(services), ILanguageMappingService
{
	public async Task Delete(IPrimaryKeyDto<int> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<int> Insert(IInsertLanguageMappingDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<ILanguageMapping>> Query(IQueryLanguageMappingsDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task<ILanguageMapping?> Select(IPrimaryKeyDto<int> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task Update(IUpdateLanguageMappingDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}
}
