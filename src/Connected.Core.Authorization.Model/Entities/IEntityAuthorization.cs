using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;

public interface IEntityAuthorization<TEntity> : IAuthorization where TEntity : IEntity
{
	Task<TEntity?> Invoke<TDto>(IEntityAuthorizationDto<TDto, TEntity> dto)
		where TDto : IDto;
}
