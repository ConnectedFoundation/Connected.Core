using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization.Entities;
public interface IEntityAuthorizationDto<TDto, TEntity> : IDto
	where TDto : IDto
	where TEntity : IEntity
{
	ICallerContext Caller { get; set; }
	TDto Dto { get; set; }
	TEntity Entity { get; set; }
}