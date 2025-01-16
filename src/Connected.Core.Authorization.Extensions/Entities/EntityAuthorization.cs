using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;

public abstract class EntityAuthorization<TEntity> : Middleware, IEntityAuthorization<TEntity>
	where TEntity : IEntity
{
	protected IDto Dto { get; private set; } = default!;
	protected TEntity Entity { get; private set; } = default!;
	protected ICallerContext Caller { get; private set; } = default!;

	public bool IsSealed { get; protected set; }

	public async Task<TEntity?> Invoke<TDto>(IEntityAuthorizationDto<TDto, TEntity> dto)
		where TDto : IDto
	{
		Dto = dto.Dto;
		Entity = dto.Entity;
		Caller = dto.Caller;

		return await OnInvoke();
	}

	protected virtual async Task<TEntity?> OnInvoke()
	{
		await Task.CompletedTask;

		return Entity;
	}
}