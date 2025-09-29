using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;

public interface IEntityAuthorization<TEntity> : IBoundAuthorization where TEntity : IEntity
{
	Task<AuthorizationResult> Invoke<TDto>(IEntityAuthorizationDto<TDto, TEntity> dto)
		where TDto : IDto;
}
