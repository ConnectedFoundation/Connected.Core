using Connected.Annotations;
using Connected.Services;

namespace Connected.Identities.Authentication;

[Service]
public interface IIdentityAuthenticationTokenService
{
	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Put)]
	Task<long?> Insert(IInsertIdentityAuthenticationTokenDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task Update(IUpdateIdentityAuthenticationTokenDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<ImmutableList<IIdentityAuthenticationToken>> Query(IQueryIdentityAuthenticationTokensDto dto);

	[ServiceOperation(ServiceOperationVerbs.Post | ServiceOperationVerbs.Get)]
	Task<IIdentityAuthenticationToken?> Select(IPrimaryKeyDto<long> dto);
}