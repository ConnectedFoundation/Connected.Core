using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Storage.Sharding.Ops;

internal sealed class Query(IShardingCache cache)
	: ServiceFunction<IQueryShardsDto, IImmutableList<IShard>>
{
	protected override async Task<IImmutableList<IShard>> OnInvoke()
	{
		return await cache.AsEntities<IShard>(f => string.Equals(f.Entity, Dto.Entity, StringComparison.OrdinalIgnoreCase));
	}
}