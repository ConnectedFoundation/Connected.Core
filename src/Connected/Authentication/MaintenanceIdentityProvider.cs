using Connected.Identities;
using Connected.Identities.Schemas;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Authentication;
internal sealed class MaintenanceIdentityProvider
	: IdentityDescriptorProvider
{
	public override string Name { get; } = "Maintenance";

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery()
	{
		if (Bootstrapper.MaintenanceIdentityToken is null)
			return ImmutableList<IIdentityDescriptor>.Empty;

		return await Task.FromResult<IImmutableList<IIdentityDescriptor>>([CreateDescriptor()]);
	}

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueDto<string> dto)
	{
		return await Task.FromResult(ImmutableList<IIdentityDescriptor>.Empty);
	}

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueListDto<string> dto)
	{
		var key = Bootstrapper.MaintenanceIdentityToken;

		if (key is null || !dto.Items.Contains(key))
			return await Task.FromResult(ImmutableList<IIdentityDescriptor>.Empty);

		return [CreateDescriptor()];
	}

	protected override async Task<IIdentityDescriptor?> OnSelect(IValueDto<string> dto)
	{
		if (string.Equals(dto.Value, Bootstrapper.MaintenanceIdentityToken, StringComparison.Ordinal))
			return await Task.FromResult(CreateDescriptor());

		return null;
	}

	private static IdentityDescriptor CreateDescriptor() => new()
	{
		Name = "Maintenance",
		Token = Bootstrapper.MaintenanceIdentityToken ?? string.Empty
	};
}
