using Connected.Membership.Claims.Dtos;
using System.Collections.Immutable;

namespace Connected.Membership.Claims;

public abstract class ClaimDescriptorProvider : Middleware, IClaimDescriptorProvider
{
	protected IQueryClaimDescriptorsDto Dto { get; private set; } = default!;
	public async Task<IImmutableList<IClaimDescriptor>> Invoke(IQueryClaimDescriptorsDto dto)
	{
		Dto = dto;

		return await OnInvoke();
	}

	protected virtual Task<IImmutableList<IClaimDescriptor>> OnInvoke()
	{
		return Task.FromResult<IImmutableList<IClaimDescriptor>>(ImmutableList<IClaimDescriptor>.Empty);
	}
}