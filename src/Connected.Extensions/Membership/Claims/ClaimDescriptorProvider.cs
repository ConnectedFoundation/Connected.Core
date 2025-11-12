using Connected.Authorization;
using Connected.Membership.Claims.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public abstract class ClaimDescriptorProvider : Middleware, IClaimDescriptorProvider
{
	protected IQueryClaimDescriptorsDto Dto { get; private set; } = default!;
	protected List<IClaimDescriptor> Claims { get; } = [];

	protected void Add(string? entity, string? entityId, string? text, string value)
	{
		Claims.Add(new ClaimDescriptor
		{
			Entity = entity ?? AuthorizationMiddleware.NullAuthorizationEntity,
			EntityId = entityId ?? AuthorizationMiddleware.NullAuthorizationEntityId,
			Value = value,
			Text = text
		});
	}

	public async Task<IImmutableList<IClaimDescriptor>> Invoke(IQueryClaimDescriptorsDto dto)
	{
		Dto = dto;

		await OnInvoke();

		return [.. Claims];
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