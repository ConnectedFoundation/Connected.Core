using Connected.Membership.Claims.Dtos;
using Connected.Membership.Claims.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

internal sealed class ClaimService(IServiceProvider services)
	: Service(services), IClaimService
{
	public async Task Delete(IPrimaryKeyDto<long> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<long> Insert(IInsertClaimDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<IClaim>> Query(IQueryClaimDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task<bool> Request(IRequestClaimDto dto)
	{
		return await Invoke(GetOperation<Request>(), dto);
	}

	public async Task<IClaim?> Select(IPrimaryKeyDto<long> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}
}
