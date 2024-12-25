using Connected.Annotations;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

[Service]
[ServiceUrl(Urls.ClaimService)]
public interface IClaimService
{
	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post)]
	Task<ImmutableList<IClaim>> Query(IQueryClaimDto dto);

	Task<bool> Request(IRequestClaimDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put | ServiceOperationVerbs.Post)]
	Task Insert(IClaimDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete | ServiceOperationVerbs.Post)]
	Task Delete(IClaimDto dto);
}