using Connected.Membership.Roles.Dtos;
using Connected.Membership.Roles.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Roles;

internal sealed class RoleService(IServiceProvider services)
	: Service(services), IRoleService
{
	public async Task Delete(IPrimaryKeyDto<int> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<int> Insert(IInsertRoleDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<IRole>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.NoPaging);
	}

	public async Task<IRole?> Select(IPrimaryKeyDto<int> id)
	{
		return await Invoke(GetOperation<Select>(), id);
	}

	public async Task<IRole?> Select(INameDto dto)
	{
		return await Invoke(GetOperation<SelectByName>(), dto);
	}

	public async Task Update(IUpdateRoleDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}
}
