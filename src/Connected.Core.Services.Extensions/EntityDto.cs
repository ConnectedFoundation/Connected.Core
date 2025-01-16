using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

public abstract class EntityDto : Dto, IEntityDto
{
	[Required, MaxLength(128)]
	public required string Entity { get; set; }

	[Required, MaxLength(128)]
	public required string EntityId { get; set; }
}
