using Connected.Services;
using Connected.Storage.Sharding.Ops;
using System.Collections.Immutable;

namespace Connected.Storage.Sharding;

internal sealed class ShardingService(IServiceProvider services)
	: Service(services), IShardingService
{
	public async Task<ImmutableList<IShard>> Query(IQueryShardsDto dto)
	{
		return await Invoke(GetOperation<Query>(), dto);
	}

	public async Task<IShard?> Select(ISelectShardDto dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task<int> Insert(IInsertShardDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task Update(IUpdateShardDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task Delete(IPrimaryKeyDto<int> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}
}