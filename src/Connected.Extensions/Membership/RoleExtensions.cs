using Connected.Membership.Dtos;
using Connected.Membership.Ops;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Membership;

internal sealed class RoleExtensions(IServiceProvider services)
		: Service(services), IRoleExtensions
{
	public async Task<IImmutableList<string>> Query(IQueryUserDto dto) => await Invoke(GetOperation<Query>(), dto);
}
