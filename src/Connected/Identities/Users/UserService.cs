using Connected.Identities.Dtos;
using Connected.Identities.Users.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Users;

internal sealed class UserService(IServiceProvider services)
	: Service(services), IUserService
{
	public async Task Delete(IPrimaryKeyDto<long> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}

	public async Task<long> Insert(IInsertUserDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task<IImmutableList<IUser>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.NoPaging);
	}

	public async Task<IImmutableList<IUser>> Query(IPrimaryKeyListDto<int> dto)
	{
		return await Invoke(GetOperation<Lookup>(), dto);
	}

	public async Task<IImmutableList<IUser>> Query(IValueListDto<string> dto)
	{
		return await Invoke(GetOperation<LookupByToken>(), dto);
	}

	public async Task<IUser?> Select(IPrimaryKeyDto<long> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task<IUser?> Select(ISelectUserDto dto)
	{
		return await Invoke(GetOperation<SelectByCredentials>(), dto);
	}

	public async Task<IUser?> Select(IValueDto<string> dto)
	{
		return await Invoke(GetOperation<Resolve>(), dto);
	}

	public async Task Update(IUpdateUserDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task Update(IUpdatePasswordDto dto)
	{
		await Invoke(GetOperation<UpdatePassword>(), dto);
	}
}
