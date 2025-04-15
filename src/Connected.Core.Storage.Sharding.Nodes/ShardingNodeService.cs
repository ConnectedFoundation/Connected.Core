using Connected.Services;
using Connected.Storage.Sharding.Nodes.Ops;
using System.Collections.Immutable;

namespace Connected.Storage.Sharding.Nodes;

internal sealed class ShardingNodeService(IServiceProvider services)
	: Service(services), IShardingNodeService
{
	public async Task<IImmutableList<IShardingNode>> Query(IQueryDto? dto)
	{
		return await Invoke(GetOperation<Query>(), dto ?? QueryDto.NoPaging);
	}

	public async Task<IShardingNode?> Select(IPrimaryKeyDto<int> dto)
	{
		return await Invoke(GetOperation<Select>(), dto);
	}

	public async Task<int> Insert(IInsertShardingNodeDto dto)
	{
		return await Invoke(GetOperation<Insert>(), dto);
	}

	public async Task Update(IUpdateShardingNodeDto dto)
	{
		await Invoke(GetOperation<Update>(), dto);
	}

	public async Task Delete(IPrimaryKeyDto<int> dto)
	{
		await Invoke(GetOperation<Delete>(), dto);
	}
}