using System.ComponentModel.DataAnnotations;

namespace Connected.Services;

internal class EntityDto : Dto, IEntityDto
{
	[Required, MaxLength(128)]
	public string Entity { get; set; } = default!;

	[Required, MaxLength(128)]
	public string EntityId { get; set; } = default!;
}
