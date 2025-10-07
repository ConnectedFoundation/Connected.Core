using Connected.Entities;
using Connected.Entities.Protection;
using Connected.Storage.Sharding.Nodes;

namespace Connected.Storage.Sharding.Protection;

internal sealed class NodeDeleteProtection(IShardingCache cache)
	: EntityProtector<IShardingNode>
{
	protected override async Task OnInvoke()
	{
		if (await cache.AsEntity(f => f.Node == Entity.Id) is not null)
			throw new InvalidOperationException(Strings.ErrEntityProtection);
	}
}