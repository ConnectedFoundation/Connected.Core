namespace Connected.Entities.Protection;

public interface IEntityProtector<TEntity> : IMiddleware
	where TEntity : IEntity
{
	Task Invoke(IEntityProtectionDto<TEntity> dto);
}
