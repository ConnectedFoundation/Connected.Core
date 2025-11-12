using Connected.Entities;
using Connected.Storage.Sharding.Nodes;

namespace Connected.Storage;
/// <summary>
/// Represents a default sharding node which is actually not a shard but
/// points to the default storage.
/// </summary>
internal sealed record DefaultShardingNode : Entity<int>, IShardingNode
{
	/// <summary>
	/// Creates a new instance of DefaultShardingNode with default properties.
	/// </summary>
	public DefaultShardingNode()
	{
		Id = 0;
		Name = "_";
		ConnectionString = "_";
		Status = Status.Enabled;
	}
	/// <inheritdoc cref="IShardingNode.Name"/>>
	public string Name { get; init; }
	/// <inheritdoc cref="IShardingNode.ConnectionString"/>>
	public string ConnectionString { get; init; }
	/// <inheritdoc cref="IShardingNode.Status"/>>
	public Status Status { get; init; }
}