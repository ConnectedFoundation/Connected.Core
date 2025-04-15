using Connected.Entities;
using Connected.Services;
using System.Collections.Immutable;

namespace Connected.Storage.Sharding.Nodes.Ops;

internal sealed class Query(IShardingNodeCache cache)
	: ServiceFunction<IQueryDto, IImmutableList<IShardingNode>>
{
	protected override async Task<IImmutableList<IShardingNode>> OnInvoke()
	{
		return await cache.AsEntities<IShardingNode>();
	}
}