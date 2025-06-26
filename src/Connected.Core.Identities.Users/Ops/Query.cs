using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Ops;

internal sealed class Query(IUserCache cache)
	: ServiceFunction<IQueryDto, IImmutableList<IUser>>
{
	protected override Task<IImmutableList<IUser>> OnInvoke()
	{
		return cache.WithDto(Dto).AsEntities<IUser>();
	}
}
