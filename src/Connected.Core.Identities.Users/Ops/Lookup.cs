using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Ops;
internal sealed class Lookup(IUserCache cache)
	: ServiceFunction<IPrimaryKeyListDto<int>, IImmutableList<IUser>>
{
	protected override Task<IImmutableList<IUser>> OnInvoke()
	{
		return cache.AsEntities(f => Dto.Items.Any(g => g == f.Id));
	}
}
