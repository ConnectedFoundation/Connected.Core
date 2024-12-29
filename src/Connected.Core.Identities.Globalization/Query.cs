using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.Globalization;

internal sealed class Query(IIdentityGlobalizationCache cache) : ServiceFunction<IQueryDto, ImmutableList<IIdentityGlobalization>>
{
	protected override async Task<ImmutableList<IIdentityGlobalization>> OnInvoke()
	{
		return await cache.WithDto(Dto).AsEntities<IIdentityGlobalization>();
	}
}