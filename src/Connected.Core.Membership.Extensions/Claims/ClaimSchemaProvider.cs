using Connected.Membership.Claims.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public abstract class ClaimSchemaProvider : Middleware, IClaimSchemaProvider
{
	protected IQueryClaimSchemaDto Dto { get; private set; } = default!;
	public async Task<IImmutableList<IClaimSchema>> Invoke(IQueryClaimSchemaDto dto)
	{
		Dto = dto;

		return await OnInvoke();
	}

	protected virtual Task<IImmutableList<IClaimSchema>> OnInvoke()
	{
		return Task.FromResult<IImmutableList<IClaimSchema>>(ImmutableList<IClaimSchema>.Empty);
	}

	protected bool DtoEquals(string? entity, string? entityId)
	{
		return string.Equals(Dto.Entity, entity, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(Dto.EntityId, entityId, StringComparison.OrdinalIgnoreCase);
	}
}