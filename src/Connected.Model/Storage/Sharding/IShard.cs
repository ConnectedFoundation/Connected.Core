using Connected.Entities;

namespace Connected.Storage.Sharding;
/// <summary>
/// Represents a sharding entry which points to the sharding node for the
/// specified parent record.
/// </summary>
/// <remarks>
/// Sharding records are always organized by its parent and all child records
/// are stored in the same node.
/// </remarks>
public interface IShard : IEntity<int>
{
	/// <summary>
	/// Specifies the node in which the data for the shard is stored.
	/// </summary>
	int Node { get; init; }
	/// <summary>
	/// Specifies the entity for which this shard is defined.
	/// </summary>
	string Entity { get; init; }
	/// <summary>
	/// Specifies the primary key, usually the Id of the entity for which this shard is defined.
	/// </summary>
	string EntityId { get; init; }
}
