using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

public abstract class EntityDto : Dto, IEntityDto
{
	[Required, MaxLength(1024)]
	public required virtual string Entity { get; set; }

	[Required, MaxLength(128)]
	public required virtual string EntityId { get; set; }
}
