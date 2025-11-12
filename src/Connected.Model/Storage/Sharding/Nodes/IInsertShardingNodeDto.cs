using Connected.Entities;
using Connected.Services;

namespace Connected.Storage.Sharding.Nodes;
public interface IInsertShardingNodeDto : IDto
{
	public string Name { get; set; }
	public string ConnectionString { get; set; }
	public Status Status { get; set; }
}