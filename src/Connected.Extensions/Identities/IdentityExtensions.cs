using Connected.Identities.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;
internal sealed class IdentityExtensions(IServiceProvider services)
		: Service(services), IIdentityExtensions
{
	public async Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto)
	{
		return await Invoke(GetOperation<QueryIdentityDescriptors>(), dto);
	}

	public async Task<IIdentityDescriptor?> Select(IValueDto<string> dto)
	{
		return await Invoke(GetOperation<SelectIdentityDescriptor>(), dto);
	}
}
