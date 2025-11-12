using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Schemas;

public abstract class IdentityDescriptorProvider : Middleware, IIdentityDescriptorProvider
{
	public abstract string Name { get; }

	public async Task<IImmutableList<IIdentityDescriptor>> Query()
	{
		return await OnQuery();
	}

	public async Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto)
	{
		return await OnQuery(dto);
	}

	public async Task<IIdentityDescriptor?> Select(IValueDto<string> dto)
	{
		return await OnSelect(dto);
	}

	public async Task<IImmutableList<IIdentityDescriptor>> Query(IValueDto<string> dto)
	{
		return await OnQuery(dto);
	}

	protected virtual async Task<IImmutableList<IIdentityDescriptor>> OnQuery()
	{
		return await Task.FromResult(ImmutableList<IIdentityDescriptor>.Empty);
	}

	protected virtual async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueListDto<string> dto)
	{
		return await Task.FromResult<IImmutableList<IIdentityDescriptor>>(ImmutableList<IIdentityDescriptor>.Empty);
	}

	protected virtual async Task<IIdentityDescriptor?> OnSelect(IValueDto<string> dto)
	{
		return await Task.FromResult<IIdentityDescriptor?>(null);
	}

	protected virtual async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueDto<string> dto)
	{
		return await Task.FromResult(ImmutableList<IIdentityDescriptor>.Empty);
	}
}