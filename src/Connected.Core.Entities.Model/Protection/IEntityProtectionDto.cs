using Connected.Services;

namespace Connected.Entities.Protection;

public interface IEntityProtectionDto<TEntity> : IDto
{
	TEntity Entity { get; set; }
	State State { get; set; }
}
