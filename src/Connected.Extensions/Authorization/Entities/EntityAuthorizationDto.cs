using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;

public class EntityAuthorizationDto<TDto, TEntity> : Dto, IEntityAuthorizationDto<TDto, TEntity>
	where TDto : IDto
	where TEntity : IEntity
{
	public required ICallerContext Caller { get; set; }
	public required TDto Dto { get; set; }
	public required TEntity Entity { get; set; }
}