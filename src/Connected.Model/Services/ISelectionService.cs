using Connected.Entities;

namespace Connected.Services;

public interface ISelectionService<TEntity, TPrimaryKey>
	where TEntity : IEntity<TPrimaryKey>
	where TPrimaryKey : notnull
{
	Task<TEntity?> Select(IPrimaryKeyDto<TPrimaryKey> dto);
}
