using Connected.Annotations;
using Connected.Entities;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Storage.Sharding.Nodes;
/// <summary>
/// A Dto used when updating an existing sharding node.
/// </summary>
public class UpdateShardingNodeDto : Dto
{
	/// <summary>
	/// An Id of the existing sharding node to be updated.
	/// </summary>
	[MinValue(1)]
	public int Id { get; set; }
	/// <inheritdoc cref="IShardingNode.Name"/>>
	[Required, MaxLength(128)]
	public string Name { get; set; } = default!;
	/// <inheritdoc cref="IShardingNode.ConnectionString"/>>
	[Required, MaxLength(1024)]
	public string ConnectionString { get; set; } = default!;
	/// <inheritdoc cref="IShardingNode.Status"/>>
	public Status Status { get; set; }
}