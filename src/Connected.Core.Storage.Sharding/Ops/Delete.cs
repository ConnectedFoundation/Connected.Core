using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage.Sharding.Ops;

internal sealed class Delete(IShardingCache cache, IStorageProvider storage, IEventService events, IShardingService shards)
	: ServiceAction<IPrimaryKeyDto<int>>
{
	protected override async Task OnInvoke()
	{
		var existing = SetState(await cache.AsEntity(f => f.Id == Dto.Id)) as Shard;

		if (existing is null)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ('{typeof(IShard).Name}')");

		await storage.Open<Shard>().Update(Dto.AsEntity<Shard>(State.Deleted));

		await cache.Remove(existing.Id);
		await events.Deleted(this, shards, existing.Id);
	}
}