using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization;
/// <summary>
/// Aggregates authorization operations for services, service operations, and entities within a scope.
/// </summary>
public interface IAuthorizationContext
{
	/// <summary>
	/// Gets a value indicating whether the most recent authorization evaluation approved the request.
	/// </summary>
	bool IsAuthorized { get; }
	/// <summary>
	/// Resets internal state so subsequent authorization checks run from a clean context.
	/// </summary>
	void Reset();
	/// <summary>
	/// Performs <c>IServiceOperationAuthorization</c> authorization.
	/// </summary>
	/// <typeparam name="TDto">Operation DTO type.</typeparam>
	/// <param name="caller">Caller context.</param>
	/// <param name="dto">Operation DTO instance.</param>
	/// <returns>Authorization decision.</returns>
	Task<AuthorizationResult> Authorize<TDto>(ICallerContext caller, TDto dto)
		where TDto : IDto;
	/// <summary>
	/// Performs <c>IEntityAuthorization</c> authorization.
	/// </summary>
	/// <typeparam name="TDto">Operation DTO type.</typeparam>
	/// <typeparam name="TEntity">Entity type.</typeparam>
	/// <param name="caller">Caller context.</param>
	/// <param name="dto">Operation DTO instance.</param>
	/// <param name="entity">Target entity instance.</param>
	/// <returns>Authorization decision.</returns>
	Task<AuthorizationResult> Authorize<TDto, TEntity>(ICallerContext caller, TDto dto, TEntity entity)
		where TDto : IDto
		where TEntity : IEntity;
	/// <summary>
	/// Determines whether an entity is protected in the current context.
	/// </summary>
	/// <typeparam name="TEntity">Entity type.</typeparam>
	/// <param name="caller">Caller context.</param>
	/// <param name="entity">Entity instance.</param>
	/// <returns>True if the entity is protected; otherwise false.</returns>
	Task<bool> IsEntityProtected<TEntity>(ICallerContext caller, TEntity entity)
		where TEntity : IEntity;
}
