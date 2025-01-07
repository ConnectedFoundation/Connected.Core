using Connected.Entities;
using Connected.Services;

namespace Connected.Storage.Sharding.Nodes.Ops;

internal sealed class Select(IShardingNodeCache cache)
	: ServiceFunction<IPrimaryKeyDto<int>, IShardingNode?>
{
	protected override async Task<IShardingNode?> OnInvoke()
	{
		return await cache.AsEntity(f => f.Id == Dto.Id);
	}
}