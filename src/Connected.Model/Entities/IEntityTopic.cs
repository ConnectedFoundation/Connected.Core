using Connected.Services;

namespace Connected.Entities;

public interface IEntityTopic<TEntity, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	Task<TPrimaryKey> Insert<TDto>(IServiceOperation<TDto> sender)
		where TDto : IDto;
	Task Update<TDto>(IServiceOperation<TDto> sender)
		where TDto : IPrimaryKeyDto<TPrimaryKey>;
	Task Patch<TUpdateDto>(IServiceOperation<IPatchDto<TPrimaryKey>> sender)
		where TUpdateDto : IPrimaryKeyDto<TPrimaryKey>;
	Task Delete(IServiceOperation<IPrimaryKeyDto<TPrimaryKey>> sender);
}
