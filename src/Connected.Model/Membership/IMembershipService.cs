using Connected.Annotations;
using Connected.Membership.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership;

[Service, ServiceUrl(MembershipUrls.MembershipService)]
public interface IMembershipService
{
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long> Insert(IInsertMembershipDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<IMembership?> Select(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<IImmutableList<IMembership>> Query(IQueryMembershipDto dto);
}
