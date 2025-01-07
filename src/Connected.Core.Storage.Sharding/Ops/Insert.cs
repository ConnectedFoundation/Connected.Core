using Connected.Entities;
using Connected.Notifications;
using Connected.Services;

namespace Connected.Storage.Sharding.Ops;

internal sealed class Insert(IShardingCache cache, IStorageProvider storage, IEventService events, IShardingService shards) : ServiceFunction<IInsertShardDto, int>
{
	protected override async Task<int> OnInvoke()
	{
		var result = await storage.Open<Shard>().Update(Dto.AsEntity<Shard>(State.New));

		if (result is null)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ('{typeof(IShard).Name}')");

		await cache.Refresh(Result);
		await events.Inserted(this, shards, result.Id);

		return result.Id;
	}
}