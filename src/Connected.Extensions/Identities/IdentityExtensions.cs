using Connected.Identities.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;

internal sealed class IdentityExtensions(IServiceProvider services)
		: Service(services), IIdentityExtensions
{
	public async Task Ping(IValueDto<string> dto) => await Invoke(GetOperation<Ping>(), dto);

	public async Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto) => await Invoke(GetOperation<QueryIdentityDescriptors>(), dto);

	public async Task<string?> Reset(IValueDto<string> dto) => await Invoke(GetOperation<Reset>(), dto);

	public async Task<IIdentityDescriptor?> Select(IValueDto<string> dto) => await Invoke(GetOperation<SelectIdentityDescriptor>(), dto);
}
