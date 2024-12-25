using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization;

public interface IAuthorizationContext
{
	/// <summary>
	/// Performs customer authorization.
	/// </summary>
	Task<IAuthorizationResult> Authorize(IAuthorizationDto dto);
	/// <summary>
	/// Performs <c>IServiceOperationAuthorization</c> authorization.
	/// </summary>
	Task Authorize<TDto>(ICallerContext caller, TDto dto)
		where TDto : IDto;
	/// <summary>
	/// Performs <c>IEntityAuthorization</c> authorization.
	/// </summary>
	Task<TEntity?> Authorize<TDto, TEntity>(ICallerContext caller, TDto dto, TEntity entity)
		where TDto : IDto
		where TEntity : IEntity;

	Task<bool> IsEntityProtected<TEntity>(ICallerContext caller, TEntity entity)
		where TEntity : IEntity;
}
