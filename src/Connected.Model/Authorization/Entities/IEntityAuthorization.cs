using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;
/// <summary>
/// Defines an authorization contract bound to a specific entity type, enabling evaluation of
/// an operation DTO against a target <typeparamref name="TEntity"/> instance.
/// </summary>
/// <typeparam name="TEntity">The entity type this authorization instance is bound to.</typeparam>
public interface IEntityAuthorization<TEntity>
	: IBoundAuthorization where TEntity : IEntity
{
	/// <summary>
	/// Invokes the authorization logic for the specified DTO and entity context.
	/// </summary>
	/// <remarks>
	/// Evaluates an authorization request represented by a DTO in the context of a specific entity
	/// instance. Implementations should inspect the caller context, DTO payload, and entity to decide
	/// whether to Pass, Fail, or Skip authorization for downstream handlers.
	/// </remarks>
	/// <typeparam name="TDto">The DTO type representing the operation being authorized.</typeparam>
	/// <param name="dto">Composite DTO containing caller context, operation data, and entity.</param>
	/// <returns>An <see cref="AuthorizationResult"/> indicating Pass, Fail, or Skip.</returns>
	Task<AuthorizationResult> Invoke<TDto>(IEntityAuthorizationDto<TDto, TEntity> dto)
		where TDto : IDto;
}
