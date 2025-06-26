using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Globalization.Ops;

internal sealed class Query(IIdentityGlobalizationCache cache) : ServiceFunction<IQueryDto, IImmutableList<IIdentityGlobalization>>
{
	protected override async Task<IImmutableList<IIdentityGlobalization>> OnInvoke()
	{
		return await cache.WithDto(Dto).AsEntities<IIdentityGlobalization>();
	}
}