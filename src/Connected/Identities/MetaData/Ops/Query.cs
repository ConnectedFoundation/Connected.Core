using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Identities.MetaData.Ops;

internal sealed class Query(IIdentityMetaDataCache cache)
  : ServiceFunction<IQueryDto, IImmutableList<IIdentityMetaData>>
{
	protected override async Task<IImmutableList<IIdentityMetaData>> OnInvoke()
	{
		return await cache.WithDto(Dto).AsEntities<IIdentityMetaData>();
	}
}
