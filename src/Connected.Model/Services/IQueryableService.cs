using Connected.Entities;

namespace Connected.Services;

public interface IQueryableService<TEntity>
	: IService
	where TEntity : IEntity
{
	IQueryable<TEntity> AsQueryable();
}
