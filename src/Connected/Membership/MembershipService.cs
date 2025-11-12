using Connected.Membership.Dtos;
using Connected.Membership.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership;

internal sealed class MembershipService(IServiceProvider services)
	: Service(services), IMembershipService
{
	public async Task Delete(IPrimaryKeyDto<long> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<long> Insert(IInsertMembershipDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<IMembership>> Query(IQueryMembershipDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task<IMembership?> Select(IPrimaryKeyDto<long> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}
}
