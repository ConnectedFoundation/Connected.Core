using Connected.Authorization;
using Connected.Membership.Claims.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public abstract class ClaimSchemaProvider : Middleware, IClaimSchemaProvider
{
	protected IQueryClaimSchemaDto Dto { get; private set; } = default!;
	protected List<IClaimSchema> Schema { get; } = [];

	protected void Add(string? entity, string? entityId, string text)
	{
		Schema.Add(new ClaimSchema
		{
			Entity = entity ?? AuthorizationMiddleware.NullAuthorizationEntity,
			EntityId = entityId ?? AuthorizationMiddleware.NullAuthorizationEntityId,
			Text = text
		});
	}
	public async Task<IImmutableList<IClaimSchema>> Invoke(IQueryClaimSchemaDto dto)
	{
		Dto = dto;

		await OnInvoke();

		return Schema.ToImmutableList();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}

	protected bool DtoEquals(string? entity)
	{
		return string.Equals(Dto.Entity, entity, StringComparison.OrdinalIgnoreCase);
	}

	protected bool DtoEquals(string? entity, string? entityId)
	{
		return string.Equals(Dto.Entity, entity, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(Dto.EntityId, entityId, StringComparison.OrdinalIgnoreCase);
	}
}