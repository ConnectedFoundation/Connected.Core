using Connected.Membership.Claims.Dtos;
using Connected.Membership.Claims.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

internal sealed class ClaimsSchemaService(IServiceProvider services)
	: Service(services), IClaimsSchemaService
{
	public async Task<IImmutableList<IClaimSchema>> Query(IQueryClaimSchemaDto dto)
	{
		return await Invoke(GetOperation<QuerySchema>(), dto);
	}

	public async Task<IImmutableList<IClaimDescriptor>> Query(IQueryClaimDescriptorsDto dto)
	{
		return await Invoke(GetOperation<QueryDescriptors>(), dto);
	}
}
