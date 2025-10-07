using Connected.Identities.Authentication.Dtos;
using Connected.Identities.Authentication.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Authentication;

internal sealed class IdentityAuthenticationTokenService(IServiceProvider services)
	: Service(services), IIdentityAuthenticationTokenService
{
	public async Task Delete(IPrimaryKeyDto<long> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<long?> Insert(IInsertIdentityAuthenticationTokenDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<IIdentityAuthenticationToken>> Query(IQueryIdentityAuthenticationTokensDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task<IIdentityAuthenticationToken?> Select(IPrimaryKeyDto<long> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task<IIdentityAuthenticationToken?> Select(IValueDto<string> dto)
	{
		return await Invoke(GetOperation<SelectByToken>(), dto);
	}

	public async Task Update(IUpdateIdentityAuthenticationTokenDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}
}
