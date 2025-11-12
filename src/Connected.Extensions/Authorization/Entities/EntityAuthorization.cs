using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;

public abstract class EntityAuthorization<TEntity> : AuthorizationMiddleware, IEntityAuthorization<TEntity>
	where TEntity : IEntity
{
	protected IDto Dto { get; private set; } = default!;
	protected TEntity Instance { get; private set; } = default!;
	protected ICallerContext Caller { get; private set; } = default!;

	public abstract string Entity { get; }
	public abstract string EntityId { get; }

	public async Task<AuthorizationResult> Invoke<TDto>(IEntityAuthorizationDto<TDto, TEntity> dto)
		where TDto : IDto
	{
		Dto = dto.Dto;
		Instance = dto.Entity;
		Caller = dto.Caller;

		return await OnInvoke();
	}

	protected virtual async Task<AuthorizationResult> OnInvoke()
	{
		return await Task.FromResult(AuthorizationResult.Skip);
	}
}