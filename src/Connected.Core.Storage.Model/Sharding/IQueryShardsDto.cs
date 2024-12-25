using Connected.Services;

namespace Connected.Storage.Sharding;
/// <summary>
/// A Dto used when querying shards for the specified entity.
/// </summary>
public interface IQueryShardsDto : IDto
{
	string Entity { get; set; }
}
