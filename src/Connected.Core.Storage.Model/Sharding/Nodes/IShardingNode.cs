using Connected.Entities;

namespace Connected.Storage.Sharding.Nodes;
/// <summary>
/// Represents a sharding node entity.
/// </summary>
/// <remarks>
/// A sharding node represents an isolated storage, usually a database where each node pointing
/// to a different database.
/// </remarks>
public interface IShardingNode : IEntity<int>
{
	/// <summary>
	/// A name of the node. This serves only for descriptive purposes.
	/// </summary>
	string Name { get; init; }
	/// <summary>
	/// The connection parameters that will be used when connecting to the node.
	/// </summary>
	string ConnectionString { get; init; }
	/// <summary>
	/// A status of the node. Disabled nodes won't cause new shards to be allocated in the node.
	/// </summary>
	Status Status { get; init; }
}
