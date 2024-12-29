using Connected.Services;

namespace Connected.Entities.Protection;

public interface IEntityProtectionDto<TEntity> : IDto
	where TEntity : IEntity
{
	TEntity Entity { get; set; }
	State State { get; set; }
}
