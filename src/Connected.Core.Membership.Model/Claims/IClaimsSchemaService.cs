using Connected.Annotations;
using Connected.Membership.Claims.Dtos;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

[Service, ServiceUrl(MembershipUrls.ClaimSchemaService)]
public interface IClaimsSchemaService
{
	[ServiceOperation(ServiceOperationVerbs.Get)]
	Task<IImmutableList<IClaimSchema>> Query(IQueryClaimSchemaDto dto);

	[ServiceOperation(ServiceOperationVerbs.Get), ServiceUrl(MembershipUrls.QueryClaimsOperation)]
	Task<IImmutableList<IClaimDescriptor>> Query(IQueryClaimDescriptorsDto dto);
}
