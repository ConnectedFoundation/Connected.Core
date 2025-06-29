using Connected.Membership.Claims.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public interface IClaimSchemaProvider : IMiddleware
{
	Task<IImmutableList<IClaimSchema>> Invoke(IQueryClaimSchemaDto dto);
}