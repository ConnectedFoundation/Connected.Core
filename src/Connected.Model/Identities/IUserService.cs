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

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.LookupOperation)]
	Task<IImmutableList<IUser>> Query(IPrimaryKeyListDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.LookupByTokenOperation)]
	Task<IImmutableList<IUser>> Query(IValueListDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IUser?> Select(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Put), ServiceUrl(IdentitiesUrls.SelectByCredentialsOperation)]
	Task<IUser?> Select(ISelectUserDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(IdentitiesUrls.ResolveOperation)]
	Task<IUser?> Select(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long> Insert(IInsertUserDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateUserDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put), ServiceUrl(IdentitiesUrls.UpdatePasswordOperation)]
	Task Update(IUpdatePasswordDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);
}