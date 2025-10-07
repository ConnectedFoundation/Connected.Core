using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities;
internal sealed class IdentityExtensions(IServiceProvider services)
		: Service(services), IIdentityExtensions
{
	public Task<IImmutableList<IIdentityDescriptor>> Query(IValueListDto<string> dto)
	{
		throw new NotImplementedException();
	}

	public Task<IIdentityDescriptor?> Select(IValueDto<string> dto)
	{
		throw new NotImplementedException();
	}
}
