using Connected.Services;

namespace Connected.Storage.Sharding.Nodes;
public interface IUpdateShardingNodeDto : IDto
{
	int Id { get; set; }
	string Name { get; set; }
	string ConnectionString { get; set; }
}
