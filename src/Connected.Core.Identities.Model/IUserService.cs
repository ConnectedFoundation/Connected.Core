using Connected.Annotations;
using Connected.Identities.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Users)]
public interface IUserService
{
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IUser>> Query(IQueryDto? dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IUser?> Select(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.SelectByCredentialsOperation)]
	Task<IUser?> Select(ISelectUserDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.ResolveOperation)]
	Task<IUser?> Select(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long> Insert(IInsertUserDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateUserDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task UpdatePassword(IUpdatePasswordDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);
}