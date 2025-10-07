using Connected.Services;

namespace Connected.Storage.Sharding;
/// <summary>
/// A Dto used when inserting a new shard.
/// </summary>
public interface IInsertShardDto : IDto
{
	int Node { get; set; }
	string Entity { get; set; }
	string EntityId { get; set; }
}