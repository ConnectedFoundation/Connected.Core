using Connected.Services;

namespace Connected.Storage.Sharding;
/// <summary>
/// A Dto used when updating an existing shard.
/// </summary>
public interface IUpdateShardDto : IDto
{
	int Id { get; set; }
	int Node { get; set; }
	string Entity { get; set; }
	string EntityId { get; set; }
}