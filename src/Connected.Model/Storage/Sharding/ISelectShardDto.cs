using Connected.Services;

namespace Connected.Storage.Sharding;
/// <summary>
/// A Dto used when selecting an existing shard.
/// </summary>
public interface ISelectShardDto : IDto
{
	string Entity { get; set; }
	string EntityId { get; set; }
}