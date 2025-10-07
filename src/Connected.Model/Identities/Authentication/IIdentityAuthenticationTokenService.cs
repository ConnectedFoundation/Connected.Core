using Connected.Annotations;
using Connected.Identities.Authentication.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Authentication;

[Service]
public interface IIdentityAuthenticationTokenService
{
	[ServiceOperation(ServiceOperationVerbs.Post)]
	Task<long?> Insert(IInsertIdentityAuthenticationTokenDto dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Update(IUpdateIdentityAuthenticationTokenDto dto);

	[ServiceOperation(ServiceOperationVerbs.Delete)]
	Task Delete(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IIdentityAuthenticationToken>> Query(IQueryIdentityAuthenticationTokensDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityAuthenticationToken?> Select(IPrimaryKeyDto<long> dto);

	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IIdentityAuthenticationToken?> Select(IValueDto<string> dto);
}