using Connected.Annotations;
using Connected.Membership.Roles.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Roles;

[Service, ServiceUrl(MembershipUrls.RoleService)]
public interface IRoleService
{
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<int> Insert(IInsertRoleDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateRoleDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IRole?> Select(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(ServiceOperations.SelectByToken)]
	Task<IRole?> Select(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(ServiceOperations.SelectByName)]
	Task<IRole?> Select(INameDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IRole>> Query(IQueryDto? dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(ServiceOperations.LookupByTokens)]
	Task<IImmutableList<IRole>> Query(IValueListDto<string> dto);
}
