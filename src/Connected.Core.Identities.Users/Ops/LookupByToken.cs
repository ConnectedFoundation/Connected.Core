using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Ops;
internal sealed class LookupByToken(IUserCache cache)
	: ServiceFunction<IValueListDto<string>, IImmutableList<IUser>>
{
	protected override async Task<IImmutableList<IUser>> OnInvoke()
	{
		return await cache.AsEntities(f => Dto.Items.Any(g => string.Equals(g, f.Token, StringComparison.OrdinalIgnoreCase)));
	}
}
