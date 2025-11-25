using Connected.Annotations;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

[Service, ServiceUrl(IdentitiesUrls.Identities)]
public interface IIdentityExtensions
{
	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl("select-descriptor")]
	Task<IIdentityDescriptor?> Select(IValueDto<string> dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl("query-descriptors")]
	Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto);
}
