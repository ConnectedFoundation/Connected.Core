using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage.Sharding.Nodes.Ops;

internal sealed class Insert(IShardingNodeCache cache, IStorageProvider storage, IEventService events, IShardingNodeService nodes)
	: ServiceFunction<IInsertShardingNodeDto, int>
{
	protected override async Task<int> OnInvoke()
	{
		var result = await storage.Open<ShardingNode>().Update(Dto.AsEntity<ShardingNode>(State.Add)) ?? throw new NullReferenceException($"{Strings.ErrEntityExpected} ('{typeof(IShardingNode).Name}')");

		await cache.Refresh(Result);
		await events.Inserted(this, nodes, result.Id);

		return result.Id;
	}
}