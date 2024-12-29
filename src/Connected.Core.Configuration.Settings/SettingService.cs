using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Configuration.Settings;

internal sealed class SettingService(IServiceProvider services) : Service(services), ISettingService
{
	public async Task<ISetting?> Select(IPrimaryKeyDto<int> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task<ISetting?> Select(INameDto dto)
	{
		return await Invoke(GetOperation<SelectByName>(), dto);
	}

	public async Task<ImmutableList<ISetting>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.Default);
	}

	public async Task Update(IUpdateSettingDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task Delete(IPrimaryKeyDto<int> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}
}