using Connected.Membership.Claims.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public interface IClaimDescriptorProvider : IMiddleware
{
	Task<IImmutableList<IClaimDescriptor>> Invoke(IQueryClaimDescriptorsDto dto);
}
