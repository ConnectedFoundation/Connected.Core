using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage.Sharding.Nodes.Ops;

internal sealed class Update(IShardingNodeCache cache, IStorageProvider storage, IEventService events, IShardingNodeService nodes)
	: ServiceAction<IUpdateShardingNodeDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await nodes.Select(Dto.AsDto<IPrimaryKeyDto<int>>())) is not ShardingNode existing)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ('{typeof(IShardingNode).Name}')");

		await storage.Open<ShardingNode>().Update(existing, Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await nodes.Select(Dto.AsDto<IPrimaryKeyDto<int>>())) as ShardingNode;
		}, Caller);

		await cache.Refresh(existing.Id);
		await events.Updated(this, nodes, existing.Id);
	}
}