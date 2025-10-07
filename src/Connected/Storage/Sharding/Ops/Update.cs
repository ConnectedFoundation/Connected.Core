using Connected.Notifications;
using Connected.Services;
using Connected.Storage.Sharding.Dtos;

namespace Connected.Storage.Sharding.Ops;

internal sealed class Update(IShardingCache cache, IStorageProvider storage, IEventService events, IShardingService shards)
	: ServiceAction<IUpdateShardDto>
{
	protected override async Task OnInvoke()
	{
		if (SetState(await shards.Select(Dto.AsDto<SelectShardDto>())) is not Shard existing)
			throw new NullReferenceException($"{Strings.ErrEntityExpected} ('{typeof(IShard).Name}')");

		await storage.Open<Shard>().Update(existing, Dto, async () =>
		{
			await cache.Refresh(Dto.Id);

			return SetState(await shards.Select(Dto.AsDto<SelectShardDto>())) as Shard;
		}, Caller);

		await cache.Refresh(existing.Id);
		await events.Updated(this, shards, existing.Id);
	}
}