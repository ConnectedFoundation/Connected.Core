using Connected.Annotations;

namespace Connected.Entities.Protection;

[Service]
public interface IEntityProtectionService
{
	Task Invoke<TEntity>(IEntityProtectionDto<TEntity> dto)
		where TEntity : IEntity;
}
