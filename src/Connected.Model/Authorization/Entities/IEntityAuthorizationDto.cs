using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;
/// <summary>
/// DTO used to carry caller context, operation DTO, and the target entity for bound authorization checks.
/// </summary>
/// <typeparam name="TDto">The operation DTO type.</typeparam>
/// <typeparam name="TEntity">The entity type the authorization is bound to.</typeparam>
public interface IEntityAuthorizationDto<TDto, TEntity>
	: IDto
	where TDto : IDto
	where TEntity : IEntity
{
	/// <summary>
	/// Gets or sets the caller context (sender and method) participating in the request.
	/// </summary>
	ICallerContext Caller { get; set; }
	/// <summary>
	/// Gets or sets the operation DTO representing the requested action.
	/// </summary>
	TDto Dto { get; set; }
	/// <summary>
	/// Gets or sets the target entity instance on which authorization is performed.
	/// </summary>
	TEntity Entity { get; set; }
}