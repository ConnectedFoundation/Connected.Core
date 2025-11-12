using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage.Sharding.Ops;

internal sealed class Delete(IShardingCache cache, IStorageProvider storage, IEventService events, IShardingService shards)
	: ServiceAction<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		var entity = SetState(await cache.AsEntity(f => f.Id == Dto.Id) as Shard).Required();

		await storage.Open<Shard>().Update(Dto.AsEntity<Shard>(State.Delete));
		await cache.Remove(entity.Id);
		await events.Deleted(this, shards, entity.Id);
	}
}