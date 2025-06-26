using Connected.Identities.Globalization.Dtos;
using Connected.Identities.Globalization.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Globalization;

internal sealed class IdentityGlobalizationService(IServiceProvider services) : Service(services), IIdentityGlobalizationService
{
	public async Task Insert(IInsertIdentityGlobalizationDto dto)
	{
		await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task Update(IUpdateIdentityGlobalizationDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task Delete(IPrimaryKeyDto<string> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<IImmutableList<IIdentityGlobalization>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.NoPaging);
	}

	public async Task<IIdentityGlobalization?> Select(IPrimaryKeyDto<string> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}
}