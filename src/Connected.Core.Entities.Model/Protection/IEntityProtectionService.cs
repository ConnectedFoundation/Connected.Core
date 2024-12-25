namespace Connected.Entities.Protection;

public interface IEntityProtectionService
{
	Task Invoke<TEntity>(IEntityProtectionDto<TEntity> dto)
		where TEntity : IEntity;
}
