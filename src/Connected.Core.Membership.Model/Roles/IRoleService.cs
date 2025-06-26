using Connected.Annotations;
using Connected.Membership.Roles.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Roles;

[Service, ServiceUrl(MembershipUrls.RoleService)]
public interface IRoleService
{
	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task<int> Insert(IInsertRoleDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateRoleDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<int> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IRole?> Select(IPrimaryKeyDto<int> id);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(MembershipUrls.SelectByNameOperation)]
	Task<IRole?> Select(INameDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IRole>> Query(IQueryDto? dto);
}
