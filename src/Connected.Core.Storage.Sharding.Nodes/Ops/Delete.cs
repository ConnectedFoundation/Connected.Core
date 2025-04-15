using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage.Sharding.Nodes.Ops;

internal sealed class Delete(IShardingNodeCache cache, IStorageProvider storage, IEventService events, IShardingNodeService nodes)
	: ServiceAction<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await nodes.Select(Dto)) is not ShardingNode existing)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ('{typeof(IShardingNode).Name}')");

		await storage.Open<ShardingNode>().Update(Dto.AsEntity<ShardingNode>(State.Delete));
		await cache.Remove(existing.Id);
		await events.Deleted(this, nodes, existing.Id);
	}
}