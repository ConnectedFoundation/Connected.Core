namespace Connected.Entities.Protection;

public abstract class EntityProtector<TEntity> : Middleware, IEntityProtector<TEntity>
	where TEntity : IEntity
{
	protected TEntity Entity { get; private set; } = default!;
	protected State State { get; private set; } = State.Default;

	public async Task Invoke(IEntityProtectionDto<TEntity> dto)
	{
		Entity = dto.Entity;
		State = dto.State;

		await OnInvoke();
	}

	protected virtual async Task OnInvoke()
	{
		await Task.CompletedTask;
	}
}