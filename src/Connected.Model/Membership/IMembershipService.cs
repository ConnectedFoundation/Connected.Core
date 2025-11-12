using Connected.Annotations;
using Connected.Membership.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership;

[Service, ServiceUrl(MembershipUrls.MembershipService)]
public interface IMembershipService
{
	Task<long> Insert(IInsertMembershipDto dto);
	Task Delete(IPrimaryKeyDto<long> dto);
	Task<IMembership?> Select(IPrimaryKeyDto<long> dto);
	Task<IImmutableList<IMembership>> Query(IQueryMembershipDto dto);
}
