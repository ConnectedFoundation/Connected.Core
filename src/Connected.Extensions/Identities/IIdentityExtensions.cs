using Connected.Annotations;
using Connected.Identities.Ops;
using Connected.Membership.Claims;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Identities)]
public interface IIdentityExtensions
{
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl("select-descriptor")]
	Task<IIdentityDescriptor?> Select(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get | ServiceOperationVerbs.Post), ServiceUrl("query-descriptors")]
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task<string?> Reset(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Put)]
	Task Ping(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl("query-claims")]
	Task<IImmutableList<IClaim>> QueryClaims();
}
