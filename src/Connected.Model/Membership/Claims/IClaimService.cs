using Connected.Annotations;
using Connected.Membership.Claims.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

[Service]
[ServiceUrl(MembershipUrls.ClaimService)]
public interface IClaimService
{
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IClaim>> Query(IQueryClaimDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<bool> Request(IRequestClaimDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IClaim?> Select(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long> Insert(IInsertClaimDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);
}