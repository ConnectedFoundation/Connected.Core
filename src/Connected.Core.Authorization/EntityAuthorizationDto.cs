using Connected.Annotations;
using Connected.Authorization.Entities;
using Connected.Entities;
using Connected.Services;

namespace Connected.Authorization;
internal sealed class EntityAuthorizationDto<TDto, TEntity> : Dto, IEntityAuthorizationDto<TDto, TEntity>
	where TDto : IDto
	where TEntity : IEntity
{
	[SkipValidation]
	public required ICallerContext Caller { get; set; }

	[SkipValidation]
	public required TDto Dto { get; set; }

	[SkipValidation]
	public required TEntity Entity { get; set; }
}
