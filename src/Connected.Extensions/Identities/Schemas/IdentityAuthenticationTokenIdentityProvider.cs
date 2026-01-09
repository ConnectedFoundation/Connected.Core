using Connected.Identities.Authentication;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Schemas;
internal sealed class IdentityAuthenticationTokenIdentityProvider(IIdentityAuthenticationTokenService authenticationTokens, IIdentityExtensions extensions)
	: IdentityDescriptorProvider
{
	public override string Name => "IdentityAuthenticationTokens";

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery()
	{
		return await Task.FromResult(ImmutableList<IIdentityDescriptor>.Empty);
	}

	protected override async Task<IImmutableList<IIdentityDescriptor>> OnQuery(IValueListDto<string> dto)
	{
		var identites = await ExtractIdentities(dto.Items);

		if (!identites.Any())
			return await Task.FromResult(ImmutableList<IIdentityDescriptor>.Empty);

		var newDto = new Dto<IValueListDto<string>>();
		newDto.Value.Items = identites.ToList();

		return await extensions.Query(newDto.Value);
	}

	protected override async Task<IIdentityDescriptor?> OnSelect(IValueDto<string> dto)
	{
		var identities = await ExtractIdentities([dto.Value]);

		if (!identities.Any())
			return null;

		return await extensions.Select(new Dto<IValueDto<string>>().CreateValue(identities.First()));
	}

	private async Task<IEnumerable<string>> ExtractIdentities(IEnumerable<string> tokens)
	{
		var retVal = new List<string>();

		foreach (var token in tokens)
		{
			var authenticationToken = await authenticationTokens.Select(new Dto().CreateValue(token));

			if (authenticationToken is null || !authenticationToken.IsValid())
				continue;

			retVal.Add(authenticationToken.Identity);
		}

		return retVal;
	}
}