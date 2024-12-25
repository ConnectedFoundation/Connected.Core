using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

[Service]
public interface IUserService
{
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ImmutableList<IUser>> Query(IQueryDto? dto);

	Task<IUser?> Select(IPrimaryKeyDto<long> dto);
	Task<IUser?> Select(ISelectUserDto dto);
	Task<IUser?> Select(IValueDto<string> dto);
	Task<bool> HasValidPassword(IPrimaryKeyDto<long> dto);

	Task<long> Insert(IInsertUserDto dto);
	Task Update(IUpdateUserDto dto);
	Task UpdatePassword(IUpdatePasswordDto dto);
	Task Delete(IPrimaryKeyDto<long> dto);
}