using Connected.Annotations;
using Connected.Services;
using System.ComponentModel.DataAnnotations;

namespace Connected.Storage.Sharding.Dtos;
internal class UpdateShardDto : Dto, IUpdateShardDto
{
	[MinValue(1)]
	public int Id { get; set; }

	[MinValue(1)]
	public int Node { get; set; }

	[Required, MaxLength(1024)]
	public required string Entity { get; set; }

	[Required, MaxLength(1024)]
	public required string EntityId { get; set; }
}
