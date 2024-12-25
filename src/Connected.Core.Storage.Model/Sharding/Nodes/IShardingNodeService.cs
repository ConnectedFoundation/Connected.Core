using Connected.Annotations;
using Connected.Services;

namespace Connected.Storage.Sharding.Nodes;
/// <summary>
/// A Service providing manipulation with the sharding nodes.
/// </summary>
/// <remarks>
/// This is an internal service. If managing of the sharding nodes should
/// be supported through the user interfaces the intermediate service layer
/// should be implemented supporting only the operations needed.
/// </remarks>
[Service]
public interface IShardingNodeService
{
	Task<ImmutableList<IShardingNode>> Query(IQueryDto? dto);
	Task<IShardingNode?> Select(IPrimaryKeyDto<int> dto);

	Task<int> Insert(IInsertShardingNodeDto dto);
	Task Update(IUpdateShardingNodeDto dto);
	Task Delete(IPrimaryKeyDto<int> dto);
}