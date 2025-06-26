using Connected.Identities.MetaData.Dtos;
using Connected.Identities.MetaData.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.MetaData;

internal sealed class IdentityMetaDataService(IServiceProvider services)
	: Service(services), IIdentityMetaDataService
{
	public async Task Delete(IPrimaryKeyDto<string> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task Insert(IInsertIdentityMetaDataDto dto)
	{
		await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<IIdentityMetaData>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.NoPaging);
	}

	public async Task<IIdentityMetaData?> Select(IPrimaryKeyDto<string> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task Update(IUpdateIdentityMetaDataDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}
}
