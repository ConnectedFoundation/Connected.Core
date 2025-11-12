using Connected.Entities;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Storage.Sharding.Nodes.Dtos;
/// <summary>
/// A Dto used when inserting a new sharding node.
/// </summary>
public class InsertShardingNodeDto : Dto, IInsertShardingNodeDto
{
	/// <inheritdoc cref="IShardingNode.Name"/>>
	[Required, MaxLength(128)]
	public string Name { get; set; } = default!;
	/// <inheritdoc cref="IShardingNode.ConnectionString"/>>
	[Required, MaxLength(1024)]
	public string ConnectionString { get; set; } = default!;
	/// <inheritdoc cref="IShardingNode.Status"/>>
	public Status Status { get; set; }
}