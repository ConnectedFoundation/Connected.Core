using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization;

public interface IAuthorizationContext
{
	bool IsAuthorized { get; }

	void Reset();
	/// <summary>
	/// Performs <c>IServiceOperationAuthorization</c> authorization.
	/// </summary>
	Task<AuthorizationResult> Authorize<TDto>(ICallerContext caller, TDto dto)
		where TDto : IDto;
	/// <summary>
	/// Performs <c>IEntityAuthorization</c> authorization.
	/// </summary>
	Task<AuthorizationResult> Authorize<TDto, TEntity>(ICallerContext caller, TDto dto, TEntity entity)
		where TDto : IDto
		where TEntity : IEntity;

	Task<bool> IsEntityProtected<TEntity>(ICallerContext caller, TEntity entity)
		where TEntity : IEntity;
}
